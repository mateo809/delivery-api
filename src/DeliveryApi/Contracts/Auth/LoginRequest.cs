using System.ComponentModel.DataAnnotations;

namespace DeliveryApi.Contracts.Auth;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);
