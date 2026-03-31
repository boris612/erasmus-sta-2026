using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using FluentValidation;
using MediatR;

namespace Events.WebAPI.Contract.Validation.Registration;

public class UpdateRegistrationValidator : AbstractValidator<UpdateCommand<RegistrationDTO>>
{
  public UpdateRegistrationValidator(IMediator mediator, IValidationMessageProvider validationMessageProvider)
  {
    var uniqueValidator = new UniqueIndexValidator<RegistrationDTO, int>(
      mediator,
      (_, _) => validationMessageProvider.UniqueRegistration(),
      t => t.EventId,
      t => t.PersonId,
      t => t.SportId);

    RuleFor(a => a.Dto.EventId).GreaterThan(0).ForeignKeyExists<UpdateCommand<RegistrationDTO>, EventDTO, int>(mediator, validationMessageProvider, validationMessageProvider.EventNotFound());
    RuleFor(a => a.Dto.PersonId).GreaterThan(0).ForeignKeyExists<UpdateCommand<RegistrationDTO>, PersonDTO, int>(mediator, validationMessageProvider, validationMessageProvider.PersonNotFound());
    RuleFor(a => a.Dto.SportId).GreaterThan(0).ForeignKeyExists<UpdateCommand<RegistrationDTO>, SportDTO, int>(mediator, validationMessageProvider, validationMessageProvider.SportNotFound());
    RuleFor(a => a.Dto).CustomAsync(uniqueValidator.ValidateExisting);
  }
}
