using Events.WebAPI.Contract.DTOs;
using FluentValidation;
using MediatR;
using MobilityOne.Common.Commands;

namespace Events.WebAPI.Contract.Validation.Person;

public class DeletePersonValidator : AbstractValidator<DeleteCommand<PersonDTO, int>>
{
  public DeletePersonValidator(IMediator mediator)
  {
    RuleFor(a => a.Id).NoChildRecords<DeleteCommand<PersonDTO, int>, RegistrationDTO, int>(nameof(RegistrationDTO.PersonId), mediator);
  }
}
