namespace DeliveryApi.Services.Errors;

public abstract class ApplicationErrorException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }

    public abstract string Title { get; }
}
