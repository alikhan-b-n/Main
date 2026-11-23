using Lama.Api.Stockists;
using Lama.Application.Expenses;
using Lama.Domain.Expenses.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

/// <summary>
/// What the business pays out: shipping to stores (also added from a delivery), Shopify,
/// the CRM and anything else. A monthly expense counts in every month from its start
/// until it ends.
/// </summary>
[ApiController]
[Route("api/expenses")]
[TypeFilter(typeof(StockistExceptionFilter))]
public class ExpensesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExpensesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>The expenses that cost something in the year (default: this year).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExpenseDto>>> GetExpenses([FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        if (year is < 2000 or > 2100)
            return BadRequest(new { message = $"Invalid year: {year}" });
        return Ok(await _mediator.Send(new GetExpensesQuery(year), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<object>> AddExpense([FromBody] SaveExpenseRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseCategory(request.Category, out var category))
            return BadRequest(InvalidCategory(request.Category));

        var id = await _mediator.Send(request.ToCommand(null, category), cancellationToken);
        return Ok(new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateExpense(Guid id, [FromBody] SaveExpenseRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseCategory(request.Category, out var category))
            return BadRequest(InvalidCategory(request.Category));

        await _mediator.Send(request.ToCommand(id, category), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteExpense(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteExpenseCommand(id), cancellationToken);
        return NoContent();
    }

    private static bool TryParseCategory(string? value, out ExpenseCategory category) =>
        Enum.TryParse(value, ignoreCase: true, out category) && Enum.IsDefined(category);

    private static object InvalidCategory(string? value) =>
        new { message = $"Invalid category: {value}. Valid values: {string.Join(", ", Enum.GetNames<ExpenseCategory>())}" };
}

/// <summary>category: Shipping, Shopify, Crm or Other. endsOn: the last month of a monthly expense, if it was stopped.</summary>
public record SaveExpenseRequest(string Category, decimal Amount, DateOnly StartsOn, bool Monthly, DateOnly? EndsOn, string? Note)
{
    public SaveExpenseCommand ToCommand(Guid? id, ExpenseCategory category) =>
        new(id, category, Amount, StartsOn, Monthly, EndsOn, Note);
}
