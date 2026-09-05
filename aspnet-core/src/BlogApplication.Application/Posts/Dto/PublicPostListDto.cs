using Abp.Application.Services.Dto;
using System;

namespace BlogApplication.Posts.Dto;

/// <summary>
/// Anonymous list view of an approved post (no content; the detail page loads it by slug).
/// </summary>
public class PublicPostListDto : EntityDto<Guid>
{
    public string Title { get; set; }

    public string Slug { get; set; }

    public string Excerpt { get; set; }

    public string AuthorUserName { get; set; }

    public DateTime? PublishedAt { get; set; }
}
