using FluentValidation;
using Lama.Application.Scheduling.Commands;
using Lama.Domain.Scheduling.Entities;

namespace Lama.Application.Scheduling.Validators;

public class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(255);

        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.TelegramUsername).MaximumLength(64);
        RuleFor(x => x.Comment).MaximumLength(Appointment.MaxCommentLength);

        RuleFor(x => x.StartUtc)
            .Must(value => value != default).WithMessage("Start time is required");
    }
}

public class CancelAppointmentCommandValidator : AbstractValidator<CancelAppointmentCommand>
{
    public CancelAppointmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(Appointment.MaxCommentLength);
    }
}

public class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.StartUtc)
            .Must(value => value != default).WithMessage("Start time is required");
    }
}

public class UpdateAvailabilitySettingsCommandValidator : AbstractValidator<UpdateAvailabilitySettingsCommand>
{
    public UpdateAvailabilitySettingsCommandValidator()
    {
        RuleFor(x => x.TimeZoneId)
            .NotEmpty().WithMessage("Time zone is required")
            .MaximumLength(64);

        RuleFor(x => x.SlotMinutes)
            .InclusiveBetween(AvailabilitySettings.MinSlotMinutes, AvailabilitySettings.MaxSlotMinutes);
        RuleFor(x => x.MinLeadTimeHours).InclusiveBetween(0, 24 * 30);
        RuleFor(x => x.MaxDaysAhead).InclusiveBetween(1, AvailabilitySettings.MaxDaysAheadLimit);
        RuleFor(x => x.BufferMinutes).InclusiveBetween(0, AvailabilitySettings.MaxSlotMinutes);

        RuleFor(x => x.Windows)
            .NotNull()
            .Must(windows => windows == null || windows.Count <= AvailabilitySettings.MaxWindows)
            .WithMessage($"No more than {AvailabilitySettings.MaxWindows} working windows are supported");

        RuleForEach(x => x.Windows).ChildRules(window =>
        {
            window.RuleFor(w => w.Day).IsInEnum();
            window.RuleFor(w => w.Start).Must(BeATime).WithMessage("Time must look like 09:30");
            window.RuleFor(w => w.End).Must(BeATime).WithMessage("Time must look like 09:30");
        });
    }

    private static bool BeATime(string? value) =>
        TimeOnly.TryParseExact(value?.Trim() ?? string.Empty, "HH:mm", out _);
}
