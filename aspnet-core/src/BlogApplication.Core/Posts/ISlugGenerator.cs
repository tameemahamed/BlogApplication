using System.Threading.Tasks;

namespace BlogApplication.Posts;

public interface ISlugGenerator
{
    /// <summary>
    /// Generates a URL-safe, unique slug from a post title, appending a
    /// numeric suffix ("-2", "-3", ...) when the base slug is already taken
    /// by a live post (prd.md E2-S3).
    /// </summary>
    Task<string> GenerateAsync(string title);
}
