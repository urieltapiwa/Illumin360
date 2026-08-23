using System.ComponentModel.DataAnnotations;

namespace Illumin360.ProfessionalWeb.Controllers;

public sealed class RegisterViewModel
{
    [Required]
    [Display(Name = "First name")]
    public string? FirstName { get; set; }

    [Required]
    [Display(Name = "Last name")]
    public string? LastName { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [MinLength(12, ErrorMessage = "Password must be at least 12 characters.")]
    public string? Password { get; set; }

    [Required]
    public string? City { get; set; }

    public string? Field { get; set; }

    public string? School { get; set; }

    public string? Role { get; set; }

    public string? Company { get; set; }
}
