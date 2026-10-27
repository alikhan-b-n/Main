using Lama.Api.Stockists;
using Lama.Application.Stockists;
using Lama.Application.Stockists.Commands;
using Lama.Application.Stockists.Queries;
using Lama.Domain.AccessControl.Entities;
using Lama.Domain.Stockists.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lama.Api.Controllers;

/// <summary>Stores that carry the books, and leads (opportunities) that might.</summary>
[ApiController]
[Route("api/stockists")]
[TypeFilter(typeof(StockistExceptionFilter))]
public class StockistsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockistsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// group: stores | opportunities (default: both). title: a book code. attention: only
    /// stores that are flagged. Not paged — a bookseller has dozens of stores.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockistListItemDto>>> GetStockists(
        [FromQuery] string? group,
        [FromQuery] string? stage,
        [FromQuery] string? title,
        [FromQuery] string? region,
        [FromQuery] string? search,
        [FromQuery] bool attention = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseOptional<StockistGroup>(group, out var parsedGroup))
            return BadRequest(Invalid<StockistGroup>("group", group));
        if (!TryParseOptional<StockistStage>(stage, out var parsedStage))
            return BadRequest(Invalid<StockistStage>("stage", stage));

        var filter = new StockistFilter(parsedGroup ?? StockistGroup.All, parsedStage, title, region, search, attention);
        return Ok(await _mediator.Send(new GetStockistsQuery(filter), cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<StockistSummaryDto>> GetSummary(CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetStockistSummaryQuery(), cancellationToken));

    /// <summary>Invoiced, paid and outstanding for a year (default: this year), by month and by store.</summary>
    [HttpGet("financials")]
    public async Task<ActionResult<StockistFinancialsDto>> GetFinancials([FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        if (year is < 2000 or > 2100)
            return BadRequest(new { message = $"Invalid year: {year}" });
        return Ok(await _mediator.Send(new GetStockistFinancialsQuery(year), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StockistDetailsDto>> GetStockist(Guid id, CancellationToken cancellationToken)
    {
        var stockist = await _mediator.Send(new GetStockistByIdQuery(id), cancellationToken);
        return stockist == null ? NotFound() : Ok(stockist);
    }

    [HttpPost]
    public async Task<ActionResult<StockistDetailsDto>> CreateStockist(
        [FromBody] SaveStockistRequest request, CancellationToken cancellationToken)
    {
        if (!TryParseOptional<StockistStage>(request.Stage, out var stage))
            return BadRequest(Invalid<StockistStage>("stage", request.Stage));

        var id = await _mediator.Send(
            new CreateStockistCommand(request.ToDetails(), stage ?? StockistStage.Lead, request.Note), cancellationToken);
        return CreatedAtAction(nameof(GetStockist), new { id },
            await _mediator.Send(new GetStockistByIdQuery(id), cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateStockist(Guid id, [FromBody] SaveStockistRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateStockistCommand(id, request.ToDetails()), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/stage")]
    public async Task<IActionResult> ChangeStage(Guid id, [FromBody] ChangeStageRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseOptional<StockistStage>(request.Stage, out var stage) || stage == null)
            return BadRequest(Invalid<StockistStage>("stage", request.Stage));

        await _mediator.Send(new ChangeStockistStageCommand(id, stage.Value), cancellationToken);
        return NoContent();
    }

    /// <summary>movement: Delivery (books dropped off), Return (taken back) or Count (what the store has now).</summary>
    [HttpPost("{id:guid}/stock")]
    public async Task<IActionResult> RecordStock(Guid id, [FromBody] RecordStockRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseOptional<StockMovement>(request.Movement, out var movement) || movement == null)
            return BadRequest(Invalid<StockMovement>("movement", request.Movement));

        await _mediator.Send(new RecordStockCommand(id, movement.Value, request.Title, request.Quantity, request.On,
            request.Note), cancellationToken);
        return NoContent();
    }

    /// <summary>contact: true for a call, email or visit (resets the "no contact" reminder).</summary>
    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] AddStockistNoteRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new AddStockistNoteCommand(id, request.Text, request.Contact, request.On),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/invoices")]
    public async Task<ActionResult<object>> AddInvoice(Guid id, [FromBody] AddInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var invoiceId = await _mediator.Send(
            new AddStockistInvoiceCommand(id, request.SentOn, request.Amount, request.Reference), cancellationToken);
        return Ok(new { id = invoiceId });
    }

    /// <summary>paid: false undoes a payment ticked by mistake.</summary>
    [HttpPatch("{id:guid}/invoices/{invoiceId:guid}")]
    public async Task<IActionResult> SetInvoicePaid(Guid id, Guid invoiceId, [FromBody] SetInvoicePaidRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetStockistInvoicePaidCommand(id, invoiceId, request.Paid, request.PaidOn),
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/invoices/{invoiceId:guid}")]
    public async Task<IActionResult> RemoveInvoice(Guid id, Guid invoiceId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveStockistInvoiceCommand(id, invoiceId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteStockist(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteStockistCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>One-off move from the Excel sheet. Stores whose name already exists are skipped.</summary>
    [HttpPost("import")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<ImportResult>> Import([FromBody] IReadOnlyList<ImportStockistRequest> request,
        CancellationToken cancellationToken)
    {
        var stockists = new List<ImportedStockist>();
        foreach (var item in request)
        {
            if (!TryParseOptional<StockistStage>(item.Stage, out var stage))
                return BadRequest(Invalid<StockistStage>("stage", item.Stage));
            stockists.Add(new ImportedStockist(item.Name, item.Region, item.Address, item.ContactName, item.Email,
                item.Phone, stage ?? StockistStage.Active, item.Stock, item.Notes, item.Invoices));
        }

        return Ok(await _mediator.Send(new ImportStockistsCommand(stockists), cancellationToken));
    }

    private static bool TryParseOptional<TEnum>(string? value, out TEnum? result) where TEnum : struct, Enum
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;
        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            return false;

        result = parsed;
        return true;
    }

    private static object Invalid<TEnum>(string name, string? value) where TEnum : struct, Enum =>
        new { message = $"Invalid {name}: {value}. Valid values: {string.Join(", ", Enum.GetNames<TEnum>())}" };
}

public record SaveStockistRequest(
    string Name,
    string? Region,
    string? Address,
    string? ContactName,
    string? Email,
    string? Phone,
    string? Stage,
    string? Note)
{
    public StockistDetails ToDetails() => new(Name, Region, Address, ContactName, Email, Phone);
}

public record ChangeStageRequest(string Stage);

public record RecordStockRequest(string Movement, string Title, int Quantity, DateOnly? On, string? Note);

public record AddStockistNoteRequest(string Text, bool Contact, DateOnly? On);

public record AddInvoiceRequest(DateOnly SentOn, decimal? Amount, string? Reference);

public record SetInvoicePaidRequest(bool Paid, DateOnly? PaidOn);

/// <summary>Same as <see cref="ImportedStockist"/>, with the stage as text ("Active", "Lead"...).</summary>
public record ImportStockistRequest(
    string Name,
    string? Region,
    string? Address,
    string? ContactName,
    string? Email,
    string? Phone,
    string? Stage,
    IReadOnlyList<ImportedStock>? Stock,
    IReadOnlyList<string>? Notes,
    IReadOnlyList<ImportedInvoice>? Invoices);
