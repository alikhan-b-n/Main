using Lama.Application.Common;
using Lama.Application.Stockists.Queries;
using Lama.Domain.Stockists.Entities;

namespace Lama.Application.Stockists.Commands;

public record CreateStockistCommand(
    StockistDetails Details,
    StockistStage Stage = StockistStage.Lead,
    string? Note = null) : ICommand<Guid>;

public class CreateStockistCommandHandler : ICommandHandler<CreateStockistCommand, Guid>
{
    private readonly IStockistRepository _stockists;
    private readonly TimeProvider _clock;

    public CreateStockistCommandHandler(IStockistRepository stockists, TimeProvider clock)
    {
        _stockists = stockists;
        _clock = clock;
    }

    public async Task<Guid> Handle(CreateStockistCommand command, CancellationToken cancellationToken)
    {
        var today = StockistClock.Today(_clock);
        var stockist = Stockist.Create(command.Details, command.Stage, today);
        if (!string.IsNullOrWhiteSpace(command.Note))
            stockist.AddNote(command.Note, today);

        await _stockists.AddAsync(stockist, cancellationToken);
        return stockist.Id;
    }
}

public record UpdateStockistCommand(Guid Id, StockistDetails Details) : ICommand;

public record ChangeStockistStageCommand(Guid Id, StockistStage Stage) : ICommand;

public enum StockMovement
{
    /// <summary>Books dropped off.</summary>
    Delivery,
    /// <summary>Unsold books taken back.</summary>
    Return,
    /// <summary>"Still 5 in stock": the quantity is what the store has now.</summary>
    Count
}

public record RecordStockCommand(
    Guid Id,
    StockMovement Movement,
    string Title,
    int Quantity,
    DateOnly? On = null,
    string? Note = null) : ICommand;

/// <summary>
/// <paramref name="IsContact"/>: a call, email or visit, which resets the "no contact"
/// reminder. Otherwise it is just a note.
/// </summary>
public record AddStockistNoteCommand(Guid Id, string Text, bool IsContact, DateOnly? On = null) : ICommand;

public record AddStockistInvoiceCommand(Guid Id, DateOnly SentOn, decimal? Amount, string? Reference) : ICommand<Guid>;

public record SetStockistInvoicePaidCommand(Guid Id, Guid InvoiceId, bool Paid, DateOnly? PaidOn = null) : ICommand;

public record RemoveStockistInvoiceCommand(Guid Id, Guid InvoiceId) : ICommand;

public record DeleteStockistCommand(Guid Id) : ICommand;

/// <summary>
/// Every change to one store follows the same steps — load, change, save — so the
/// handlers share them here and differ only in the change itself.
/// </summary>
public class StockistCommandHandlers :
    ICommandHandler<UpdateStockistCommand>,
    ICommandHandler<ChangeStockistStageCommand>,
    ICommandHandler<RecordStockCommand>,
    ICommandHandler<AddStockistNoteCommand>,
    ICommandHandler<AddStockistInvoiceCommand, Guid>,
    ICommandHandler<SetStockistInvoicePaidCommand>,
    ICommandHandler<RemoveStockistInvoiceCommand>,
    ICommandHandler<DeleteStockistCommand>
{
    private readonly IStockistRepository _stockists;
    private readonly TimeProvider _clock;

    public StockistCommandHandlers(IStockistRepository stockists, TimeProvider clock)
    {
        _stockists = stockists;
        _clock = clock;
    }

    public Task Handle(UpdateStockistCommand command, CancellationToken cancellationToken) =>
        Change(command.Id, s => s.UpdateDetails(command.Details), cancellationToken);

    public Task Handle(ChangeStockistStageCommand command, CancellationToken cancellationToken) =>
        Change(command.Id, s => s.ChangeStage(command.Stage, Today), cancellationToken);

    public Task Handle(RecordStockCommand command, CancellationToken cancellationToken)
    {
        var on = command.On ?? Today;
        return Change(command.Id, s =>
        {
            switch (command.Movement)
            {
                case StockMovement.Delivery:
                    s.RecordDelivery(command.Title, command.Quantity, on, command.Note);
                    break;
                case StockMovement.Return:
                    s.RecordReturn(command.Title, command.Quantity, on, command.Note);
                    break;
                default:
                    s.RecordStockCount(command.Title, command.Quantity, on, command.Note);
                    break;
            }
        }, cancellationToken);
    }

    public Task Handle(AddStockistNoteCommand command, CancellationToken cancellationToken)
    {
        var on = command.On ?? Today;
        return Change(command.Id, s =>
        {
            if (command.IsContact)
                s.LogContact(command.Text, on);
            else
                s.AddNote(command.Text, on);
        }, cancellationToken);
    }

    public async Task<Guid> Handle(AddStockistInvoiceCommand command, CancellationToken cancellationToken)
    {
        var invoiceId = Guid.Empty;
        await Change(command.Id, s => invoiceId = s.AddInvoice(command.SentOn, command.Amount, command.Reference).Id,
            cancellationToken);
        return invoiceId;
    }

    public Task Handle(SetStockistInvoicePaidCommand command, CancellationToken cancellationToken) =>
        Change(command.Id, s =>
        {
            if (command.Paid)
                s.MarkInvoicePaid(command.InvoiceId, command.PaidOn ?? Today);
            else
                s.MarkInvoiceUnpaid(command.InvoiceId);
        }, cancellationToken);

    public Task Handle(RemoveStockistInvoiceCommand command, CancellationToken cancellationToken) =>
        Change(command.Id, s => s.RemoveInvoice(command.InvoiceId), cancellationToken);

    public async Task Handle(DeleteStockistCommand command, CancellationToken cancellationToken)
    {
        var stockist = await _stockists.GetByIdAsync(command.Id, cancellationToken)
                       ?? throw new StockistNotFoundException(command.Id);
        await _stockists.DeleteAsync(stockist, cancellationToken);
    }

    private DateOnly Today => StockistClock.Today(_clock);

    private async Task Change(Guid id, Action<Stockist> change, CancellationToken cancellationToken)
    {
        var stockist = await _stockists.GetByIdAsync(id, cancellationToken)
                       ?? throw new StockistNotFoundException(id);
        change(stockist);
        await _stockists.SaveChangesAsync(cancellationToken);
    }
}
