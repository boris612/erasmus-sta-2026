using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using FluentValidation;

namespace Events.WebAPI.Benchmarks;

internal sealed class SimplifiedAddRegistrationValidator : AbstractValidator<AddCommand<RegistrationDTO, int>>
{
  public SimplifiedAddRegistrationValidator()
  {
    RuleFor(a => a.Dto.EventId).GreaterThan(0);
    RuleFor(a => a.Dto.PersonId).GreaterThan(0);
    RuleFor(a => a.Dto.SportId).GreaterThan(0);
  }
}
