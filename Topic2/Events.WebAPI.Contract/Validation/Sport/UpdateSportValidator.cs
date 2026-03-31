using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using FluentValidation;
using MediatR;

namespace Events.WebAPI.Contract.Validation.Sport;

public class UpdateSportValidator : AbstractValidator<UpdateCommand<SportDTO>>
{
  public UpdateSportValidator(IMediator mediator, IValidationMessageProvider validationMessageProvider)
  {
    var uniqueIndexValidator = new UniqueIndexValidator<SportDTO, int>(
      mediator,
      (_, values) => validationMessageProvider.UniqueSportName(values[0]),
      t => t.Name);

    RuleFor(a => a.Dto.Name).NotEmpty().MaximumLength(100).DependentRules(() =>
       RuleFor(a => a.Dto).CustomAsync(uniqueIndexValidator.ValidateExisting));
  }
}
