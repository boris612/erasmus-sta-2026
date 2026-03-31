using Events.WebAPI.Contract.DTOs;
using FluentValidation;
using MediatR;
using MobilityOne.Common.Commands;

namespace Events.WebAPI.Contract.Validation.Event;

public class DeleteEventValidator : AbstractValidator<DeleteCommand<EventDTO, int>>
{
  public DeleteEventValidator(IMediator mediator)
  {
    RuleFor(a => a.Id).NoChildRecords<DeleteCommand<EventDTO, int>, RegistrationDTO, int>(nameof(RegistrationDTO.EventId), mediator);
  }
}
