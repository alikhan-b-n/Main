using FluentValidation;
using Lama.Application.CustomerManagement.Commands;

namespace Lama.Application.CustomerManagement.Validators;

public class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Organization name is required")
            .MaximumLength(200).WithMessage("Organization name must not exceed 200 characters");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid organization type");

        RuleFor(x => x.Size)
            .IsInEnum().WithMessage("Invalid organization size");

        RuleFor(x => x.LegalName)
            .MaximumLength(200).WithMessage("Legal name must not exceed 200 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.LegalName));

        RuleFor(x => x.Industry)
            .MaximumLength(100).WithMessage("Industry must not exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Industry));

        RuleFor(x => x.Website)
            .MaximumLength(255).WithMessage("Website must not exceed 255 characters")
            .Must(BeAValidUrl).WithMessage("Website must be a valid URL")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}