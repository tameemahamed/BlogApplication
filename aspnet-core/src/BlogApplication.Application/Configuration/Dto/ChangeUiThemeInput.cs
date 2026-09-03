using System.ComponentModel.DataAnnotations;

namespace BlogApplication.Configuration.Dto;

public class ChangeUiThemeInput
{
    [Required]
    [StringLength(32)]
    public string Theme { get; set; }
}
