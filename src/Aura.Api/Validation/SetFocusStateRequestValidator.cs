using Aura.Api.Endpoints;
using Aura.Domain.FocusState;
using FluentValidation;

namespace Aura.Api.Validation;

/// <summary>
/// Validator for <see cref="SetFocusStateRequest"/>.
/// When State is provided, it must be a valid <see cref="FocusStateType"/> value.
/// </summary>
public sealed class SetFocusStateRequestValidator : AbstractValidator<SetFocusStateRequest>
{
    public SetFocusStateRequestValidator()
    {
        When(r => r.State is not null, () =>
        {
            RuleFor(r => r.State)
                .Must(state => Enum.TryParse<FocusStateType>(state, ignoreCase: true, out _))
                .WithMessage(state =>
                    $"'State' must be one of: {string.Join(", ", Enum.GetNames<FocusStateType>())}. " +
                    $"'{state.State}' is not valid.");
        });
    }
}
