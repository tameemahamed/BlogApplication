using Abp.Application.Services;
using BlogApplication.Comments.Dto;
using System;
using System.Threading.Tasks;

namespace BlogApplication.Comments;

public interface ICommentAppService : IApplicationService
{
    Task<CommentDto> CreateCommentAsync(CreateCommentInput input);

    Task<CommentDto> CreateReplyAsync(CreateReplyInput input);

    Task UpdateCommentAsync(UpdateCommentInput input);

    Task DeleteCommentAsync(Guid id);

    Task<CommentThreadDto> GetCommentThreadAsync(GetCommentThreadInput input);
}
