using Events.WebAPI.Contract.DTOs;
using FluentValidation;
using MediatR;
using MobilityOne.Common.Commands;

namespace Events.WebAPI.Contract.Validation.Sport;

public class DeleteSportValidator : AbstractValidator<DeleteCommand<SportDTO, int>>
{
  public DeleteSportValidator(IMediator mediator)
  {      
    RuleFor(a => a.Id).NoChildRecords<DeleteCommand<SportDTO, int>, RegistrationDTO, int>(nameof(RegistrationDTO.SportId), mediator);     
  }
}
