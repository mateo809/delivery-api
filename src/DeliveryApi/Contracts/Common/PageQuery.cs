using System.ComponentModel.DataAnnotations;

namespace DeliveryApi.Contracts.Common;

public class PageQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
