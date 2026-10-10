using FluentValidation;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Users;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(User.UsernameMaxLength)
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("Username may only contain letters, digits, '.', '_' and '-'.");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(User.FullNameMaxLength);
        RuleFor(x => x.Password).SetValidator(new PasswordValidator());
        RuleFor(x => x.Role).IsInEnum();
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(User.FullNameMaxLength);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.Password!).SetValidator(new PasswordValidator()).When(x => !string.IsNullOrEmpty(x.Password));
    }
}

public sealed class PasswordValidator : AbstractValidator<string>
{
    public PasswordValidator()
    {
        RuleFor(x => x).NotEmpty().WithName("Password")
            .MinimumLength(6).WithName("Password")
            .MaximumLength(100).WithName("Password");
    }
}
