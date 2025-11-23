using AutoMapper;
using RevloDB.Entities;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.CreatedNamespaces, opt => opt.MapFrom(src =>
                src.CreatedNamespaces.Where(n => !n.IsDeleted)))
            .ForMember(dest => dest.UserNamespaces, opt => opt.MapFrom(src =>
                src.UserNamespaces.Where(un => !un.Namespace.IsDeleted)));
    }
}
