namespace DeliveryApi.Services.Errors;

public sealed class NotFoundException(string message) : ApplicationErrorException(message)
{
    public override int StatusCode => StatusCodes.Status404NotFound;

    public override string Title => "Resource not found";
}
