using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using FluentValidation;

namespace Events.WebAPI.Contract.Validation.Event;

public class AddEventValidator : AbstractValidator<AddCommand<EventDTO, int>>
{
  public AddEventValidator()
  {
    RuleFor(a => a.Dto.Name).NotEmpty().MaximumLength(150);
    RuleFor(a => a.Dto.EventDate).NotEmpty();
  }
}
