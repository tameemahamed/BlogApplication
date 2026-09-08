using System;

namespace BlogApplication.Comments.Dto;

/// <summary>
/// A comment in the moderation overview feed (prd.md E6-S6) - carries the
/// post title so moderators see the context without fetching each post.
/// </summary>
public class ModerationCommentDto : CommentDto
{
    public string PostTitle { get; set; }
}
