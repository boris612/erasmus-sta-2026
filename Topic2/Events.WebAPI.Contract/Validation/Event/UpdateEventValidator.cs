using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using FluentValidation;

namespace Events.WebAPI.Contract.Validation.Event;

public class UpdateEventValidator : AbstractValidator<UpdateCommand<EventDTO>>
{
  public UpdateEventValidator()
  {
    RuleFor(a => a.Dto.Name).NotEmpty().MaximumLength(150);
    RuleFor(a => a.Dto.EventDate).NotEmpty();
  }
}
