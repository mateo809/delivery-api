namespace DeliveryApi.Tests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<DeliveryApiFactory>
{
    public const string Name = "Api";
}
