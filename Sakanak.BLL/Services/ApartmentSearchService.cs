using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Apartment;
using Sakanak.BLL.DTOs.Common;
using Sakanak.BLL.DTOs.Search;
using Sakanak.BLL.Interfaces;
using Sakanak.DAL.Data;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;
using SearchApartmentListItemDto = Sakanak.BLL.DTOs.Search.ApartmentListItemDto;

namespace Sakanak.BLL.Services;

public class ApartmentSearchService : IApartmentSearchService
{
    private readonly SakanakDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public ApartmentSearchService(SakanakDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<Result<PagedResult<SearchApartmentListItemDto>>> SearchApartmentsAsync(ApartmentSearchDto searchDto)
    {
        if (searchDto.MinPrice.HasValue && searchDto.MaxPrice.HasValue && searchDto.MaxPrice < searchDto.MinPrice)
        {
            return Result<PagedResult<SearchApartmentListItemDto>>.Failure("Maximum price must be greater than or equal to minimum price.");
        }

        var pageNumber = Math.Max(1, searchDto.PageNumber);
        var maxPageSize = Math.Max(1, _configuration.GetValue("Search:MaxPageSize", 50));
        var defaultPageSize = Math.Max(1, _configuration.GetValue("Search:DefaultPageSize", 12));
        var pageSize = Math.Clamp(searchDto.PageSize <= 0 ? defaultPageSize : searchDto.PageSize, 1, maxPageSize);

        var query = VisibleApartmentsQuery();

        if (!string.IsNullOrWhiteSpace(searchDto.City))
        {
            var city = searchDto.City.Trim().ToLower();
            query = query.Where(apartment => apartment.City.ToLower().Contains(city));
        }

        if (searchDto.MinPrice.HasValue)
        {
            query = query.Where(apartment => apartment.PricePerMonth >= searchDto.MinPrice.Value);
        }

        if (searchDto.MaxPrice.HasValue)
        {
            query = query.Where(apartment => apartment.PricePerMonth <= searchDto.MaxPrice.Value);
        }

        if (searchDto.MinSeats.HasValue)
        {
            query = query.Where(apartment => apartment.TotalSeats >= searchDto.MinSeats.Value);
        }

        if (searchDto.MaxSeats.HasValue)
        {
            query = query.Where(apartment => apartment.TotalSeats <= searchDto.MaxSeats.Value);
        }

        query = (searchDto.SortBy.ToLowerInvariant(), searchDto.Descending) switch
        {
            ("price", false) => query.OrderBy(apartment => apartment.PricePerMonth),
            ("price", true) => query.OrderByDescending(apartment => apartment.PricePerMonth),
            ("city", false) => query.OrderBy(apartment => apartment.City),
            ("city", true) => query.OrderByDescending(apartment => apartment.City),
            ("seats", false) => query.OrderBy(apartment => apartment.TotalSeats),
            ("seats", true) => query.OrderByDescending(apartment => apartment.TotalSeats),
            ("createddate", false) => query.OrderBy(apartment => apartment.ApartmentId),
            _ => query.OrderByDescending(apartment => apartment.ApartmentId)
        };

        var apartments = await query.ToListAsync();

        var selectedAmenities = searchDto.Amenities
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .ToList();

        if (selectedAmenities.Count > 0)
        {
            apartments = apartments
                .Where(apartment => selectedAmenities.All(required =>
                    apartment.Amenities.Any(actual => actual.Equals(required, StringComparison.OrdinalIgnoreCase))))
                .ToList();
        }

        var totalCount = apartments.Count;
        var items = apartments
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(MapListItem)
            .ToList();

        return Result<PagedResult<SearchApartmentListItemDto>>.Success(new PagedResult<SearchApartmentListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    public async Task<Result<ApartmentDetailDto>> GetApartmentDetailsAsync(int apartmentId, int studentId)
    {
        var studentExists = await _dbContext.Students.AnyAsync(item => item.StudentId == studentId);
        if (!studentExists)
        {
            return Result<ApartmentDetailDto>.Failure("Student profile was not found.");
        }

        var apartment = await VisibleApartmentsQuery()
            .FirstOrDefaultAsync(item => item.ApartmentId == apartmentId);

        if (apartment is null)
        {
            return Result<ApartmentDetailDto>.Failure("Apartment was not found or is no longer available.");
        }

        var occupiedSeats = apartment.Bookings.Count(item => item.Status == BookingStatus.Accepted);
        return Result<ApartmentDetailDto>.Success(new ApartmentDetailDto
        {
            ApartmentId = apartment.ApartmentId,
            Address = apartment.Address,
            City = apartment.City,
            PricePerMonth = apartment.PricePerMonth,
            TotalSeats = apartment.TotalSeats,
            AvailableSeats = Math.Max(0, apartment.TotalSeats - occupiedSeats),
            Amenities = apartment.Amenities.ToList(),
            VirtualTourUrl = apartment.VirtualTourUrl,
            Photos = apartment.Media
                .Where(media => media.Type == MediaType.Image)
                .Select(media => new ApartmentMediaDto
                {
                    MediaId = media.MediaId,
                    Url = media.Url,
                    Type = media.Type.ToString()
                })
                .ToList(),
            LandlordName = apartment.Landlord.ApplicationUser.Name,
            LandlordApplicationUserId = apartment.Landlord.ApplicationUserId,
            LandlordPhoneNumber = apartment.Landlord.ApplicationUser.PhoneNumber,
            LandlordVerified = apartment.Landlord.VerificationStatus
        });
    }

    public async Task<Result<List<string>>> GetAvailableCitiesAsync()
    {
        var cities = await VisibleApartmentsQuery()
            .Select(apartment => apartment.City)
            .Distinct()
            .OrderBy(city => city)
            .ToListAsync();

        return Result<List<string>>.Success(cities);
    }

    public async Task<Result<List<string>>> GetAvailableAmenitiesAsync()
    {
        var configuredAmenities = _configuration.GetSection("Amenities").Get<string[]>() ?? Array.Empty<string>();
        if (configuredAmenities.Length > 0)
        {
            return Result<List<string>>.Success(configuredAmenities.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(item => item).ToList());
        }

        var apartments = await VisibleApartmentsQuery().Select(apartment => apartment.Amenities).ToListAsync();
        var amenities = apartments
            .SelectMany(item => item)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item)
            .ToList();

        return Result<List<string>>.Success(amenities);
    }

    private IQueryable<Apartment> VisibleApartmentsQuery()
        => _dbContext.Apartments
            .Include(apartment => apartment.Media)
            .Include(apartment => apartment.Requests)
            .Include(apartment => apartment.Bookings)
            .Include(apartment => apartment.Landlord)
                .ThenInclude(landlord => landlord.ApplicationUser)
            .Where(apartment =>
                apartment.IsActive &&
                apartment.Landlord.ApplicationUser.Status == UserStatus.Active &&
                apartment.Requests
                    .OrderByDescending(request => request.CreatedAt)
                    .Select(request => (RequestStatus?)request.Status)
                    .FirstOrDefault() == RequestStatus.Approved);

    private static SearchApartmentListItemDto MapListItem(Apartment apartment)
        => new()
        {
            ApartmentId = apartment.ApartmentId,
            Address = apartment.Address,
            City = apartment.City,
            PricePerMonth = apartment.PricePerMonth,
            TotalSeats = apartment.TotalSeats,
            PrimaryPhotoUrl = apartment.Media
                .Where(media => media.Type == MediaType.Image)
                .Select(media => media.Url)
                .FirstOrDefault(),
            Amenities = apartment.Amenities.ToList(),
            LandlordName = apartment.Landlord.ApplicationUser.Name
        };
}
