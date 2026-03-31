using FluentValidation;
using MediatR;
using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;

namespace Events.WebAPI.Contract.Validation.Sport;

public class AddSportValidator : AbstractValidator<AddCommand<SportDTO, int>>
{
  public AddSportValidator(IMediator mediator, IValidationMessageProvider validationMessageProvider)
  {
    var uniqueIndexValidator = new UniqueIndexValidator<SportDTO, int>(
      mediator,
      (_, values) => validationMessageProvider.UniqueSportName(values[0]),
      t => t.Name);

    RuleFor(a => a.Dto.Name).NotEmpty().MaximumLength(100).DependentRules(() =>
       RuleFor(a => a.Dto).CustomAsync(uniqueIndexValidator.Validate));
  }
}
