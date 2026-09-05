namespace BlogApplication.Authorization;

public static class PermissionNames
{
    public const string Pages_Users = "Pages.Users";
    public const string Pages_Users_Activation = "Pages.Users.Activation";

    public const string Pages_Roles = "Pages.Roles";

    public static class Blog
    {
        public const string Posts_Create = "Pages.Blog.Posts.Create";
        public const string Posts_Edit = "Pages.Blog.Posts.Edit";
        public const string Posts_Delete = "Pages.Blog.Posts.Delete";
        public const string Posts_Archive = "Pages.Blog.Posts.Archive";
        public const string Posts_Approve = "Pages.Blog.Posts.Approve";

        public const string Comments_Create = "Pages.Blog.Comments.Create";
        public const string Comments_Edit = "Pages.Blog.Comments.Edit";
        public const string Comments_Delete = "Pages.Blog.Comments.Delete";

        public const string Replies_Create = "Pages.Blog.Replies.Create";

        public const string Upvotes_Toggle = "Pages.Blog.Upvotes.Toggle";

        public const string Bans_Manage = "Pages.Blog.Bans.Manage";
    }
}
