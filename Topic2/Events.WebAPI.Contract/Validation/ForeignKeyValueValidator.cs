using FluentValidation;
using MediatR;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.Queries.Generic;

namespace Events.WebAPI.Contract.Validation;

public static class ForeignKeyValueValidatorExtension 
{   
  public static IRuleBuilderOptions<TCommand, TPK> ForeignKeyExists<TCommand, TDto, TPK>(
    this IRuleBuilder<TCommand, TPK> ruleBuilder,
    IMediator mediator,
    IValidationMessageProvider validationMessageProvider,
    ValidationMessage? validationMessage = null)
    where TDto : IHasIdAsPK<TPK>
    where TPK : IEquatable<TPK>
  {
    ValidationMessage message = validationMessage ?? validationMessageProvider.ForeignKeyNotFound("{PropertyName}");

    return ruleBuilder.MustAsync(new ForeignKeyValueValidator<TCommand, TDto, TPK>(mediator).Validate)
                      .WithMessage(message.Message)
                      .WithErrorCode(message.Code);
  }

  private class ForeignKeyValueValidator<TCommand, TDto, TPK> where TDto : IHasIdAsPK<TPK> where TPK : IEquatable<TPK>
  {      
    private readonly IMediator mediator;

    public ForeignKeyValueValidator(IMediator mediator)
    {
      this.mediator = mediator;
    }

    public async Task<bool> Validate(TCommand command, TPK value, ValidationContext<TCommand> validationContext, CancellationToken cancellationToken)
    {
      var query = new DoesItemExistsQuery<TDto, TPK>(value);
      try
      {
        bool itemExists = await mediator.Send(query, cancellationToken);
        return itemExists;
      }
      catch (Exception exc)
      {
        validationContext.AddFailure(exc.Message);
        return false;
      }
    }
  }
}
