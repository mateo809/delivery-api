namespace DeliveryApi.Services.Errors;

public sealed class AuthenticationFailedException(string message) : ApplicationErrorException(message)
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;

    public override string Title => "Authentication failed";
}
