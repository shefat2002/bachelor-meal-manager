using MealManager.Application.Common;
using MealManager.Application.Features.Bazar;
using MealManager.Application.Features.Utilities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealManager.Api.Controllers;

/// <summary>Expense streams: bazar items (P2-BE-2) and utility payments (P2-BE-4).</summary>
[ApiController]
[Authorize]
[Route("api/expenses")]
public sealed class ExpensesController(ISender sender) : ControllerBase
{
    [HttpPost("bazar")]
    public async Task<ActionResult<IReadOnlyList<BazarExpenseDto>>> CreateBazar(
        [FromBody] CreateBazarExpensesRequest request) =>
        CreatedAtAction(nameof(ListBazar), await sender.Send(
            new CreateBazarExpensesCommand(request.Date, request.BuyerMemberId, request.Items)));

    [HttpGet("bazar")]
    public async Task<ActionResult<PaginatedResult<BazarExpenseDto>>> ListBazar(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] Guid? buyerMemberId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir) =>
        Ok(await sender.Send(new GetBazarExpensesQuery(page, pageSize, buyerMemberId, from, to, sortBy, sortDir)));

    [HttpPost("utility")]
    public async Task<ActionResult<UtilityPaymentDto>> CreateUtility(
        [FromBody] CreateUtilityPaymentRequest request) =>
        CreatedAtAction(nameof(ListUtility), await sender.Send(
            new CreateUtilityPaymentCommand(request.UtilityTypeId, request.Amount, request.PaymentDate, request.Note)));

    [HttpGet("utility")]
    public async Task<ActionResult<PaginatedResult<UtilityPaymentDto>>> ListUtility(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] Guid? utilityTypeId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? sortBy,
        [FromQuery] string? sortDir) =>
        Ok(await sender.Send(new GetUtilityPaymentsQuery(page, pageSize, utilityTypeId, from, to, sortBy, sortDir)));
}
