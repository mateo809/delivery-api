namespace DeliveryApi.Services.Errors;

public sealed class ConflictException(string message) : ApplicationErrorException(message)
{
    public override int StatusCode => StatusCodes.Status409Conflict;

    public override string Title => "Conflict";
}
