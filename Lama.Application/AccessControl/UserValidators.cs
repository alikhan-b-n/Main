using FluentValidation;
using Lama.Domain.AccessControl.Entities;

namespace Lama.Application.AccessControl;

internal static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Password is required")
            .MinimumLength(CrmUser.MinPasswordLength)
            .WithMessage($"Password must be at least {CrmUser.MinPasswordLength} characters")
            .MaximumLength(CrmUser.MaxPasswordLength);
}

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(255);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.Password).Password();
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.Role).IsInEnum();
    }
}

public class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator()
    {
        RuleFor(x => x.NewPassword).Password();
    }
}

public class ChangeOwnPasswordCommandValidator : AbstractValidator<ChangeOwnPasswordCommand>
{
    public ChangeOwnPasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).Password();
    }
}

public class AuthenticateUserCommandValidator : AbstractValidator<AuthenticateUserCommand>
{
    public AuthenticateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(255);
        // No length rule here: an old short password must still be able to sign in
        RuleFor(x => x.Password).NotEmpty().MaximumLength(CrmUser.MaxPasswordLength);
    }
}
