using Abp.Application.Services.Dto;
using System;

namespace BlogApplication.Comments.Dto;

public class CommentDto : EntityDto<Guid>
{
    public Guid PostId { get; set; }

    public long UserId { get; set; }

    public string UserName { get; set; }

    public Guid? ParentCommentId { get; set; }

    public string ContentMarkdown { get; set; }

    public bool IsEdited { get; set; }

    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Populated by CommentAppService (prd.md E5-S3). The flag is null for
    /// anonymous callers (prd.md A1).
    /// </summary>
    public int UpvoteCount { get; set; }

    public bool? UpvotedByCurrentUser { get; set; }
}
