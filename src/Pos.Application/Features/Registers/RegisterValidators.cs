using FluentValidation;
using Pos.Application.Common.Validation;
using Pos.Domain.Entities;

namespace Pos.Application.Features.Registers;

public sealed class OpenRegisterRequestValidator : AbstractValidator<OpenRegisterRequest>
{
    public OpenRegisterRequestValidator()
    {
        RuleFor(x => x.OpeningCash).Money();
        RuleFor(x => x.Notes).MaximumLength(RegisterSession.NotesMaxLength);
    }
}

public sealed class CloseRegisterRequestValidator : AbstractValidator<CloseRegisterRequest>
{
    public CloseRegisterRequestValidator()
    {
        RuleFor(x => x.ClosingCash).Money();
        RuleFor(x => x.Notes).MaximumLength(RegisterSession.NotesMaxLength);
    }
}
