using DeliveryApi.Auth;
using DeliveryApi.Contracts.Common;
using DeliveryApi.Contracts.Orders;
using DeliveryApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryApi.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class OrdersController(OrderService orderService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await orderService.CreateAsync(User.ToCurrentUser(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { orderId = order.Id }, order);
    }

    [HttpGet]
    [ProducesResponseType<PagedResponse<OrderSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<OrderSummaryResponse>>> List(
        [FromQuery] OrderListQuery query,
        CancellationToken cancellationToken) =>
        Ok(await orderService.ListAsync(User.ToCurrentUser(), query, cancellationToken));

    [HttpGet("available")]
    [Authorize(Roles = Roles.Courier)]
    [ProducesResponseType<PagedResponse<OrderSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<OrderSummaryResponse>>> ListAvailable(
        [FromQuery] PageQuery query,
        CancellationToken cancellationToken) =>
        Ok(await orderService.ListAvailableAsync(query, cancellationToken));

    [HttpGet("{orderId:guid}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(Guid orderId, CancellationToken cancellationToken) =>
        Ok(await orderService.GetAsync(User.ToCurrentUser(), orderId, cancellationToken));

    [HttpGet("{orderId:guid}/history")]
    [ProducesResponseType<IReadOnlyList<OrderStatusChangeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OrderStatusChangeResponse>>> GetHistory(
        Guid orderId,
        CancellationToken cancellationToken) =>
        Ok(await orderService.GetHistoryAsync(User.ToCurrentUser(), orderId, cancellationToken));

    [HttpPost("{orderId:guid}/cancel")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Cancel(Guid orderId, CancellationToken cancellationToken) =>
        Ok(await orderService.CancelAsync(User.ToCurrentUser(), orderId, cancellationToken));

    [HttpPost("{orderId:guid}/accept")]
    [Authorize(Roles = Roles.Courier)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Accept(Guid orderId, CancellationToken cancellationToken) =>
        Ok(await orderService.AcceptAsync(User.ToCurrentUser(), orderId, cancellationToken));

    [HttpPost("{orderId:guid}/pickup")]
    [Authorize(Roles = Roles.Courier)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> PickUp(Guid orderId, CancellationToken cancellationToken) =>
        Ok(await orderService.PickUpAsync(User.ToCurrentUser(), orderId, cancellationToken));

    [HttpPost("{orderId:guid}/deliver")]
    [Authorize(Roles = Roles.Courier)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Deliver(Guid orderId, CancellationToken cancellationToken) =>
        Ok(await orderService.DeliverAsync(User.ToCurrentUser(), orderId, cancellationToken));
}
