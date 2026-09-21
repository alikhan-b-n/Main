using FluentValidation;
using Lama.Application.LeadManagement.Commands;
using Lama.Domain.LeadManagement.Entities;

namespace Lama.Application.LeadManagement.Validators;

public class SubmitTelegramLeadCommandValidator : AbstractValidator<SubmitTelegramLeadCommand>
{
    // Option codes are short slugs; free-text answers are capped at 300 characters by the bot.
    private const int CodeLength = 64;
    private const int FreeTextLength = 300;
    private const int MaxListItems = 20;

    public SubmitTelegramLeadCommandValidator()
    {
        RuleFor(x => x.Submission).NotNull();

        RuleFor(x => x.Submission.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(100);

        RuleFor(x => x.Submission.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(255);

        RuleFor(x => x.Submission.Phone).MaximumLength(20);
        RuleFor(x => x.Submission.TelegramUsername).MaximumLength(CodeLength);
        RuleFor(x => x.Submission.Source).MaximumLength(CodeLength);
        RuleFor(x => x.Submission.ExternalId).MaximumLength(CodeLength);
        RuleFor(x => x.Submission.Score).InclusiveBetween(0, 100);
        RuleFor(x => x.Submission.SurveySummary).MaximumLength(4000);

        RuleFor(x => x.Submission.Qualification).NotNull().ChildRules(q =>
        {
            q.RuleFor(v => v.ApplicantType).MaximumLength(CodeLength);
            q.RuleFor(v => v.AgeRange).MaximumLength(CodeLength);
            q.RuleFor(v => v.CurrentEducation).MaximumLength(CodeLength);
            q.RuleFor(v => v.TargetDegree).MaximumLength(CodeLength);
            q.RuleFor(v => v.IntakeYear).MaximumLength(CodeLength);
            q.RuleFor(v => v.FundingNeed).MaximumLength(CodeLength);
            q.RuleFor(v => v.AnnualBudget).MaximumLength(CodeLength);
            q.RuleFor(v => v.EnglishLevel).MaximumLength(CodeLength);
            q.RuleFor(v => v.EnglishCertificate).MaximumLength(CodeLength);
            q.RuleFor(v => v.Gpa).MaximumLength(FreeTextLength);
            q.RuleFor(v => v.EnglishScore).MaximumLength(FreeTextLength);
            q.RuleFor(v => v.FieldsOfInterest).MaximumLength(FreeTextLength);

            q.RuleFor(v => v.TargetCountries)
                .Must(list => list == null || list.Count <= MaxListItems)
                .WithMessage($"No more than {MaxListItems} countries");
            q.RuleForEach(v => v.TargetCountries).MaximumLength(CodeLength);

            q.RuleFor(v => v.ServicesNeeded)
                .Must(list => list == null || list.Count <= MaxListItems)
                .WithMessage($"No more than {MaxListItems} services");
            q.RuleForEach(v => v.ServicesNeeded).MaximumLength(CodeLength);
        });
    }
}

public class ChangeLeadStatusCommandValidator : AbstractValidator<ChangeLeadStatusCommand>
{
    public ChangeLeadStatusCommandValidator()
    {
        RuleFor(x => x.Status).IsInEnum();

        RuleFor(x => x.LostReason)
            .NotEmpty().WithMessage("A reason is required when a lead is lost")
            .When(x => x.Status == LeadStatus.Lost);

        RuleFor(x => x.LostReason).MaximumLength(500);
    }
}

public class AddLeadNoteCommandValidator : AbstractValidator<AddLeadNoteCommand>
{
    public AddLeadNoteCommandValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Note cannot be empty")
            .MaximumLength(LeadEvent.MaxTextLength);
    }
}
