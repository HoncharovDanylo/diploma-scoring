using System.ComponentModel.DataAnnotations;

namespace Identity.Application.Models;

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}
