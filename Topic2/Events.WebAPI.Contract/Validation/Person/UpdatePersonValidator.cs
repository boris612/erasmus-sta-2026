using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.Validation;
using FluentValidation;
using MediatR;

namespace Events.WebAPI.Contract.Validation.Person;

public class UpdatePersonValidator : AbstractValidator<UpdateCommand<PersonDTO>>
{
  public UpdatePersonValidator(IMediator mediator, IValidationMessageProvider validationMessageProvider)
  {
    var uniqueIndexValidator = new UniqueIndexValidator<PersonDTO, int>(
      mediator,
      (_, _) => validationMessageProvider.UniquePersonDocumentAndCountry(),
      t => t.DocumentNumber,
      t => t.CountryCode);

    RuleFor(a => a.Dto.FirstName).NotEmpty().MaximumLength(100);
    RuleFor(a => a.Dto.LastName).NotEmpty().MaximumLength(100);
    RuleFor(a => a.Dto.FirstNameTranscription).NotEmpty().MaximumLength(100);
    RuleFor(a => a.Dto.LastNameTranscription).NotEmpty().MaximumLength(100);
    RuleFor(a => a.Dto.AddressLine).NotEmpty().MaximumLength(200);
    RuleFor(a => a.Dto.PostalCode).NotEmpty().MaximumLength(20);
    RuleFor(a => a.Dto.City).NotEmpty().MaximumLength(100);
    RuleFor(a => a.Dto.AddressCountry).NotEmpty().MaximumLength(100);
    RuleFor(a => a.Dto.Email).NotEmpty().MaximumLength(255).EmailAddress();
    RuleFor(a => a.Dto.ContactPhone).NotEmpty().MaximumLength(50);
    RuleFor(a => a.Dto.BirthDate).NotEmpty();
    RuleFor(a => a.Dto.DocumentNumber).NotEmpty().MaximumLength(50);
    RuleFor(a => a.Dto.CountryCode).NotEmpty().MaximumLength(3);

    RuleFor(a => a.Dto).CustomAsync(uniqueIndexValidator.ValidateExisting);
  }
}
