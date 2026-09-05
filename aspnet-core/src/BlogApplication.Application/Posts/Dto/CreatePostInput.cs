using System.ComponentModel.DataAnnotations;

namespace BlogApplication.Posts.Dto;

public class CreatePostInput
{
    [Required]
    [MaxLength(PostConsts.MaxTitleLength)]
    public string Title { get; set; }

    [MaxLength(PostConsts.MaxExcerptLength)]
    public string Excerpt { get; set; }

    [Required]
    [MaxLength(PostConsts.MaxContentLength)]
    public string ContentMarkdown { get; set; }
}
