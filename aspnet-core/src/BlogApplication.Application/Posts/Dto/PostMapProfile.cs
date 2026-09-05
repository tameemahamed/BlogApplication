using AutoMapper;

namespace BlogApplication.Posts.Dto;

public class PostMapProfile : Profile
{
    public PostMapProfile()
    {
        CreateMap<Post, PostDto>();
        CreateMap<Post, PublicPostListDto>();
        CreateMap<Post, PublicPostDetailDto>();
    }
}
