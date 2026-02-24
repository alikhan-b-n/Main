using FluentValidation;
using Lama.Application.CustomerManagement.Commands;

namespace Lama.Application.CustomerManagement.Validators;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters");

        RuleFor(x => x.Industry)
            .MaximumLength(100).WithMessage("Industry must not exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Industry));

        RuleFor(x => x.Website)
            .MaximumLength(255).WithMessage("Website must not exceed 255 characters")
            .Must(BeAValidUrl).WithMessage("Website must be a valid URL")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));

        RuleFor(x => x.Domain)
            .MaximumLength(255).WithMessage("Domain must not exceed 255 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Domain));
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
