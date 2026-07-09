using Microsoft.EntityFrameworkCore;
using Sakanak.BLL.DTOs;
using Sakanak.BLL.DTOs.Apartment;
using Sakanak.BLL.Interfaces;
using Sakanak.DAL.Data;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.Services;

public class ApartmentAssignmentService : IApartmentAssignmentService
{
    private readonly SakanakDbContext _dbContext;

    public ApartmentAssignmentService(SakanakDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<StudentApartmentAssignmentDto>> GetStudentCurrentApartmentAsync(int studentId)
    {
        var contract = await ActiveContractsQuery()
            .FirstOrDefaultAsync(item => item.StudentId == studentId);

        if (contract is null)
        {
            return Result<StudentApartmentAssignmentDto>.Success(new StudentApartmentAssignmentDto());
        }

        var occupancy = await BuildOccupancyAsync(contract.Apartment);
        var roommates = await GetRoommatesAsync(contract.ApartmentId, contract.Student);
        var paymentId = await _dbContext.Payments
            .Where(item => item.ContractId == contract.ContractId && item.Status == PaymentStatus.Paid)
            .Select(item => (int?)item.PaymentId)
            .FirstOrDefaultAsync();

        var today = DateTime.UtcNow.Date;
        return Result<StudentApartmentAssignmentDto>.Success(new StudentApartmentAssignmentDto
        {
            HasAssignment = true,
            ApartmentId = contract.ApartmentId,
            ContractId = contract.ContractId,
            PaymentId = paymentId,
            Address = contract.Apartment.Address,
            City = contract.Apartment.City,
            MonthlyRent = contract.Booking.PricePerMonthAtBooking > 0 ? contract.Booking.PricePerMonthAtBooking : contract.Apartment.PricePerMonth,
            TotalSeats = contract.Apartment.TotalSeats,
            OccupiedSeats = occupancy.OccupiedSeats,
            AvailableSeats = occupancy.AvailableSeats,
            OccupancyRate = occupancy.OccupancyRate,
            Amenities = contract.Booking.AmenitiesAtBooking.Length > 0 ? contract.Booking.AmenitiesAtBooking : contract.Apartment.Amenities,
            VirtualTourUrl = contract.Apartment.VirtualTourUrl,
            PhotoUrls = contract.Apartment.Media.Where(item => item.Type == MediaType.Image).Select(item => item.Url).ToList(),
            LandlordName = contract.Landlord.ApplicationUser.Name,
            LandlordEmail = contract.Landlord.ApplicationUser.Email ?? string.Empty,
            LandlordPhoneNumber = contract.Landlord.ApplicationUser.PhoneNumber,
            LandlordApplicationUserId = contract.Landlord.ApplicationUserId,
            ContractStartDate = contract.StartDate,
            ContractEndDate = contract.EndDate,
            DaysUntilMoveIn = Math.Max(0, (contract.StartDate.Date - today).Days),
            DaysRemaining = Math.Max(0, (contract.EndDate.Date - today).Days),
            Roommates = roommates
        });
    }

    public async Task<Result<List<ApartmentOccupancyDto>>> GetLandlordApartmentsWithOccupancyAsync(int landlordId)
    {
        var apartments = await ApartmentsQuery()
            .Where(item => item.LandlordId == landlordId)
            .OrderBy(item => item.Address)
            .ToListAsync();

        var results = new List<ApartmentOccupancyDto>();
        foreach (var apartment in apartments)
        {
            results.Add(await BuildOccupancyAsync(apartment));
        }

        return Result<List<ApartmentOccupancyDto>>.Success(results);
    }

    public async Task<Result<ApartmentOccupancyDto>> GetApartmentTenantsAsync(int apartmentId, int landlordId)
    {
        var query = ApartmentsQuery().Where(item => item.ApartmentId == apartmentId);
        if (landlordId > 0)
        {
            query = query.Where(item => item.LandlordId == landlordId);
        }
        var apartment = await query.FirstOrDefaultAsync();

        return apartment is null
            ? Result<ApartmentOccupancyDto>.Failure("Apartment was not found.")
            : Result<ApartmentOccupancyDto>.Success(await BuildOccupancyAsync(apartment, includeTenants: true));
    }

    public async Task<Result<List<ApartmentOccupancyDto>>> GetAllApartmentsWithOccupancyAsync()
    {
        var apartments = await ApartmentsQuery().OrderBy(item => item.City).ThenBy(item => item.Address).ToListAsync();
        var results = new List<ApartmentOccupancyDto>();
        foreach (var apartment in apartments)
        {
            results.Add(await BuildOccupancyAsync(apartment, includeTenants: true));
        }

        return Result<List<ApartmentOccupancyDto>>.Success(results);
    }

    private IQueryable<Contract> ActiveContractsQuery()
        => _dbContext.Contracts
            .Include(item => item.Booking)
            .Include(item => item.Student).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Apartment).ThenInclude(item => item.Media)
            .Include(item => item.Landlord).ThenInclude(item => item.ApplicationUser)
            .Where(item => item.Status == ContractStatus.Active);

    private IQueryable<Apartment> ApartmentsQuery()
        => _dbContext.Apartments
            .Include(item => item.Landlord).ThenInclude(item => item.ApplicationUser)
            .Include(item => item.Media);

    private async Task<ApartmentOccupancyDto> BuildOccupancyAsync(Apartment apartment, bool includeTenants = false)
    {
        var group = await _dbContext.ApartmentGroups
            .Include(g => g.Students).ThenInclude(s => s.ApplicationUser)
            .Include(g => g.Students).ThenInclude(s => s.Questionnaire)
            .FirstOrDefaultAsync(g => g.ApartmentId == apartment.ApartmentId);

        var tenants = includeTenants && group != null
            ? group.Students.Select(student => MapRoommate(student)).ToList()
            : new List<RoommateDto>();

        var occupiedSeats = group?.Students.Count ?? 0;
        var availableSeats = Math.Max(0, apartment.TotalSeats - occupiedSeats);
        var occupancyRate = apartment.TotalSeats <= 0 ? 0 : Math.Round((decimal)occupiedSeats / apartment.TotalSeats * 100, 1);

        var activeContracts = await ActiveContractsQuery()
            .Where(item => item.ApartmentId == apartment.ApartmentId)
            .ToListAsync();

        return new ApartmentOccupancyDto
        {
            ApartmentId = apartment.ApartmentId,
            Address = apartment.Address,
            City = apartment.City,
            LandlordName = apartment.Landlord.ApplicationUser.Name,
            PricePerMonth = apartment.PricePerMonth,
            TotalSeats = apartment.TotalSeats,
            OccupiedSeats = occupiedSeats,
            AvailableSeats = availableSeats,
            OccupancyRate = occupancyRate,
            IsActive = apartment.IsActive,
            PrimaryPhotoUrl = apartment.Media.Where(item => item.Type == MediaType.Image).Select(item => item.Url).FirstOrDefault(),
            MonthlyRevenue = activeContracts.Sum(item => item.Booking.PricePerMonthAtBooking > 0 ? item.Booking.PricePerMonthAtBooking : item.Apartment.PricePerMonth),
            Tenants = tenants
        };
    }

    private async Task<List<RoommateDto>> GetRoommatesAsync(int apartmentId, Student currentStudent)
    {
        var contracts = await ActiveContractsQuery()
            .Where(item => item.ApartmentId == apartmentId && item.StudentId != currentStudent.StudentId)
            .ToListAsync();

        return contracts.Select(item => MapRoommate(item.Student, CalculateCompatibility(currentStudent.Questionnaire, item.Student.Questionnaire))).ToList();
    }

    private static RoommateDto MapRoommate(Student student, decimal compatibilityScore = 0)
        => new()
        {
            StudentId = student.StudentId,
            Name = student.ApplicationUser.Name,
            University = student.University,
            Faculty = student.Faculty,
            Email = student.ApplicationUser.Email ?? string.Empty,
            PhoneNumber = student.ApplicationUser.PhoneNumber,
            CompatibilityScore = compatibilityScore,
            LifestyleSummary = student.Questionnaire is null
                ? "Lifestyle profile incomplete"
                : $"{(student.Questionnaire.IsSmoker ? "Smoker" : "Non-smoker")}, {student.Questionnaire.SleepSchedule}, Hygiene {student.Questionnaire.HygieneLevel}/5"
        };

    private static decimal CalculateCompatibility(LifestyleQuestionnaire? first, LifestyleQuestionnaire? second)
    {
        if (first is null || second is null) return 50;
        if ((first.GenderPreference == GenderPreference.Male && second.GenderPreference == GenderPreference.Female) ||
            (first.GenderPreference == GenderPreference.Female && second.GenderPreference == GenderPreference.Male)) return 0;

        var sleep = first.SleepSchedule == second.SleepSchedule ? 100m : first.SleepSchedule == SleepSchedule.Flexible || second.SleepSchedule == SleepSchedule.Flexible ? 75m : 20m;
        var smoker = first.IsSmoker == second.IsSmoker ? 100m : 30m;
        var hygiene = Math.Max(0, 100 - Math.Abs(first.HygieneLevel - second.HygieneLevel) * 20);
        var study = first.StudyHabits == second.StudyHabits ? 100m : 50m;
        var social = first.SocialPreference == second.SocialPreference ? 100m : first.SocialPreference is SocialPreference.Moderate or SocialPreference.Ambivert || second.SocialPreference is SocialPreference.Moderate or SocialPreference.Ambivert ? 70m : 30m;
        return Math.Round((sleep * .25m) + (smoker * .25m) + (hygiene * .20m) + (study * .15m) + (social * .15m), 1);
    }
}
