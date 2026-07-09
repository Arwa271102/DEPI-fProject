using AutoMapper;
using Sakanak.BLL.DTOs.Request;
using Sakanak.Domain.Entities;
using Sakanak.Domain.Enums;

namespace Sakanak.BLL.MappingProfiles;

public class RequestProfile : Profile
{
    public RequestProfile()
    {
        CreateMap<Request, RequestListItemDto>()
            .ForMember(destination => destination.ApartmentAddress, options => options.MapFrom(source => source.Apartment.Address))
            .ForMember(destination => destination.City, options => options.MapFrom(source => source.Apartment.City))
            .ForMember(destination => destination.LandlordName, options => options.MapFrom(source => source.Landlord.ApplicationUser.Name))
            .ForMember(destination => destination.LandlordEmail, options => options.MapFrom(source => source.Landlord.ApplicationUser.Email ?? string.Empty))
            .ForMember(destination => destination.Status, options => options.MapFrom(source => source.Status.ToString()))
            .ForMember(destination => destination.Type, options => options.MapFrom(source => source.Type.ToString()))
            .ForMember(destination => destination.ReviewedByAdminName, options => options.MapFrom(source => source.ReviewedByAdmin != null ? source.ReviewedByAdmin.ApplicationUser.Name : null))
            .ForMember(destination => destination.ThumbnailUrl, options => options.MapFrom(source =>
                source.Apartment.Media.Where(media => media.Type == MediaType.Image).Select(media => media.Url).FirstOrDefault()));

        CreateMap<Request, RequestDetailsDto>()
            .ForMember(destination => destination.Status, options => options.MapFrom(source => source.Status.ToString()))
            .ForMember(destination => destination.Type, options => options.MapFrom(source => source.Type.ToString()))
            .ForMember(destination => destination.LandlordName, options => options.MapFrom(source => source.Landlord.ApplicationUser.Name))
            .ForMember(destination => destination.LandlordEmail, options => options.MapFrom(source => source.Landlord.ApplicationUser.Email ?? string.Empty))
            .ForMember(destination => destination.LandlordPhoneNumber, options => options.MapFrom(source => source.Landlord.ApplicationUser.PhoneNumber))
            .ForMember(destination => destination.ReviewedByAdminName, options => options.MapFrom(source => source.ReviewedByAdmin != null ? source.ReviewedByAdmin.ApplicationUser.Name : null));
    }
}
