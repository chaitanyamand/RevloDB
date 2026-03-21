using AutoMapper;
using RevloDB.DTOs;
using RevloDB.Entities;

namespace RevloDB.Mappers
{
    public class VersioningProfile : Profile
    {
        public VersioningProfile()
        {
            CreateMap<Branch, BranchDto>()
                .ForMember(dest => dest.HeadCommitHash, opt => opt.MapFrom(src =>
                    src.HeadCommit != null ? src.HeadCommit.Hash : null));

            CreateMap<Commit, CommitDto>()
                .ForMember(dest => dest.Author, opt => opt.MapFrom(src =>
                    src.AuthorUser != null ? src.AuthorUser.Username : "Unknown"));

            CreateMap<CommitChange, CommitChangeDto>()
                .ForMember(dest => dest.Action, opt => opt.MapFrom(src => src.Action.ToString()));
        }
    }
}
