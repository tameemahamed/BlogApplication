namespace BlogApplication.Bans;

/// <summary>
/// The granular ban types (prd.md E6, schema 5.3). Each maps 1:1 to the
/// prohibited permission (Comment -> Comments.Create, Reply ->
/// Replies.Create, Upvote -> Upvotes.Toggle) so bans stay independent
/// (prd.md A6) - a comment ban does not block replying or upvoting.
/// </summary>
public enum BanType
{
    Comment = 0,
    Reply = 1,
    Upvote = 2
}
