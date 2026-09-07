using AutoMapper;

namespace BlogApplication.Comments.Dto;

public class CommentMapProfile : Profile
{
    public CommentMapProfile()
    {
        CreateMap<Comment, CommentDto>();
        CreateMap<Comment, TopLevelCommentDto>()
            .ForMember(d => d.Replies, opt => opt.Ignore());
    }
}
