using Microsoft.IdentityModel.JsonWebTokens;

namespace DeliveryApi.Auth;

public static class AppClaimTypes
{
    public const string UserId = JwtRegisteredClaimNames.Sub;

    public const string Email = JwtRegisteredClaimNames.Email;

    public const string Role = "role";
}
