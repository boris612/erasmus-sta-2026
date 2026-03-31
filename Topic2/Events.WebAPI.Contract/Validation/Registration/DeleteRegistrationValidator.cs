using Events.WebAPI.Contract.DTOs;
using FluentValidation;
using MediatR;
using MobilityOne.Common.Commands;

namespace Events.WebAPI.Contract.Validation.Registration;

public class DeleteRegistrationValidator : AbstractValidator<DeleteCommand<RegistrationDTO, int>>
{
  public DeleteRegistrationValidator(IMediator mediator)
  {
  }
}
