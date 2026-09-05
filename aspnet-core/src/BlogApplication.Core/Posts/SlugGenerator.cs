using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlogApplication.Posts;

public class SlugGenerator : DomainService, ISlugGenerator
{
    private readonly IRepository<Post, Guid> _postRepository;

    public SlugGenerator(IRepository<Post, Guid> postRepository)
    {
        _postRepository = postRepository;
    }

    public async Task<string> GenerateAsync(string title)
    {
        var baseSlug = Slugify(title);
        var slug = baseSlug;
        var suffix = 2;

        // Soft-deleted posts are excluded by the global query filter, so their
        // slugs are free for reuse (prd.md D7 - partial unique index).
        while (await _postRepository.GetAll().AnyAsync(p => p.Slug == slug))
        {
            slug = baseSlug + "-" + suffix;
            suffix++;
        }

        return slug;
    }

    private static string Slugify(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "post";
        }

        var builder = new StringBuilder();
        foreach (var c in title.Trim().ToLowerInvariant())
        {
            builder.Append(char.IsLetterOrDigit(c) ? c : '-');
        }

        var slug = builder.ToString();

        // collapse repeated separators and trim them from the ends
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        slug = slug.Trim('-');

        // leave room for the uniqueness suffix within MaxSlugLength
        if (slug.Length > PostConsts.MaxSlugLength - 6)
        {
            slug = slug.Substring(0, PostConsts.MaxSlugLength - 6).Trim('-');
        }

        return string.IsNullOrEmpty(slug) ? "post" : slug;
    }
}
