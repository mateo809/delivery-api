using System.Security.Claims;
using DeliveryApi.Domain;
using DeliveryApi.Services.Errors;

namespace DeliveryApi.Auth;

public static class ClaimsPrincipalExtensions
{
    public static CurrentUser ToCurrentUser(this ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(AppClaimTypes.UserId);
        var role = principal.FindFirstValue(AppClaimTypes.Role);

        if (!Guid.TryParse(userId, out var id) || !Enum.TryParse<UserRole>(role, out var userRole))
        {
            throw new AuthenticationFailedException("The access token does not identify a valid user.");
        }

        return new CurrentUser(id, userRole);
    }
}
