using System.ComponentModel.DataAnnotations;
using BCrypt.Net;

namespace MockPaymentService.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }

    // Methods for secure password handling
    public void SetPassword(string password)
    {
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, PasswordHash);
    }
}