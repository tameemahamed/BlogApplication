using System.ComponentModel.DataAnnotations;

namespace BlogApplication.Users.Dto;

public class ChangeUserLanguageDto
{
    [Required]
    public string LanguageName { get; set; }
}