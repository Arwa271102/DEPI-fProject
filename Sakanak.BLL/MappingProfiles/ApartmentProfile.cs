using AutoMapper;
using Sakanak.BLL.DTOs.Apartment;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.MappingProfiles;

public class ApartmentProfile : Profile
{
    public ApartmentProfile()
    {
        CreateMap<Media, ApartmentMediaDto>()
            .ForMember(destination => destination.Type, options => options.MapFrom(source => source.Type.ToString()));

        CreateMap<CreateApartmentDto, Apartment>()
            .ForMember(destination => destination.Amenities, options => options.MapFrom(source => source.Amenities.ToArray()))
            .ForMember(destination => destination.Media, options => options.Ignore())
            .ForMember(destination => destination.Requests, options => options.Ignore())
            .ForMember(destination => destination.ApartmentGroups, options => options.Ignore())
            .ForMember(destination => destination.Bookings, options => options.Ignore())
            .ForMember(destination => destination.Contracts, options => options.Ignore())
            .ForMember(destination => destination.Landlord, options => options.Ignore())
            .ForMember(destination => destination.IsActive, options => options.MapFrom(_ => false));

        CreateMap<Apartment, ApartmentListItemDto>()
            .ForMember(destination => destination.LatestRequestStatus, options => options.MapFrom(source =>
                source.Requests.OrderByDescending(request => request.CreatedAt).Select(request => request.Status.ToString()).FirstOrDefault() ?? "No Request"))
            .ForMember(destination => destination.LatestRequestDate, options => options.MapFrom(source =>
                source.Requests.OrderByDescending(request => request.CreatedAt).Select(request => (DateTime?)request.CreatedAt).FirstOrDefault()))
            .ForMember(destination => destination.PrimaryPhotoUrl, options => options.MapFrom(source =>
                source.Media.Where(media => media.Type == MediaType.Image).Select(media => media.Url).FirstOrDefault()))
            .ForMember(destination => destination.OccupiedSeats, options => options.MapFrom(source =>
                source.Bookings.Count(booking => booking.Status == BookingStatus.Accepted)))
            .ForMember(destination => destination.AvailableSeats, options => options.MapFrom(source =>
                Math.Max(0, source.TotalSeats - source.Bookings.Count(booking => booking.Status == BookingStatus.Accepted))))
            .ForMember(destination => destination.ActiveBookingCount, options => options.MapFrom(source =>
                source.Bookings.Count(booking => booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Accepted)));

        CreateMap<Apartment, ApartmentDetailsDto>()
            .ForMember(destination => destination.Amenities, options => options.MapFrom(source => source.Amenities.ToList()))
            .ForMember(destination => destination.LatestRequestStatus, options => options.MapFrom(source =>
                source.Requests.OrderByDescending(request => request.CreatedAt).Select(request => request.Status.ToString()).FirstOrDefault() ?? "No Request"))
            .ForMember(destination => destination.LatestRequestMessage, options => options.MapFrom(source =>
                source.Requests.OrderByDescending(request => request.CreatedAt).Select(request => request.Message).FirstOrDefault()))
            .ForMember(destination => destination.LatestRequestDate, options => options.MapFrom(source =>
                source.Requests.OrderByDescending(request => request.CreatedAt).Select(request => (DateTime?)request.CreatedAt).FirstOrDefault()))
            .ForMember(destination => destination.LatestResolvedAt, options => options.MapFrom(source =>
                source.Requests.OrderByDescending(request => request.CreatedAt).Select(request => request.ResolvedAt).FirstOrDefault()))
            .ForMember(destination => destination.Photos, options => options.MapFrom(source =>
                source.Media.Where(media => media.Type == MediaType.Image)))
            .ForMember(destination => destination.OccupiedSeats, options => options.MapFrom(source =>
                source.Bookings.Count(booking => booking.Status == BookingStatus.Accepted)))
            .ForMember(destination => destination.AvailableSeats, options => options.MapFrom(source =>
                Math.Max(0, source.TotalSeats - source.Bookings.Count(booking => booking.Status == BookingStatus.Accepted))))
            .ForMember(destination => destination.ActiveBookingCount, options => options.MapFrom(source =>
                source.Bookings.Count(booking => booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Accepted)));

        CreateMap<UpdateApartmentDto, Apartment>()
            .ForMember(destination => destination.Amenities, options => options.MapFrom(source => source.Amenities.ToArray()))
            .ForMember(destination => destination.Media, options => options.Ignore())
            .ForMember(destination => destination.Requests, options => options.Ignore())
            .ForMember(destination => destination.ApartmentGroups, options => options.Ignore())
            .ForMember(destination => destination.Bookings, options => options.Ignore())
            .ForMember(destination => destination.Contracts, options => options.Ignore())
            .ForMember(destination => destination.Landlord, options => options.Ignore());
    }
}
