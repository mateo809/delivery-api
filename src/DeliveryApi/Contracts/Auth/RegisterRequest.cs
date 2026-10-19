using System.ComponentModel.DataAnnotations;
using DeliveryApi.Domain;

namespace DeliveryApi.Contracts.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password,
    [Required, MaxLength(100)] string FullName,
    [Required] UserRole? Role);
