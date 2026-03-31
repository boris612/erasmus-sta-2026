using Events.WebAPI.Contract.Queries.Generic;
using FluentValidation;
using MediatR;

namespace Events.WebAPI.Contract.Validation;

public static class NoChildRecordsValidatorExtension
{   
  public static IRuleBuilderOptions<TCommand, TPK> NoChildRecords<TCommand, TDto, TPK>(this IRuleBuilder<TCommand, TPK> ruleBuilder, string columnName, IMediator mediator)     
  {
    return ruleBuilder.MustAsync(new NoChildRecordsValidator<TDto, TPK>(columnName, mediator).Validate)
                      .WithMessage("Cannot delete entity {PropertyValue} because there are child records in table related to " + typeof(TDto).Name.ToString());
  }

  private class NoChildRecordsValidator<TDto, TPK>
  {
    private readonly string columnName;
    private readonly IMediator mediator;

    public NoChildRecordsValidator(string columnName, IMediator mediator)
    {
      this.columnName = columnName;
      this.mediator = mediator;
    }

    public async Task<bool> Validate(TPK value, CancellationToken cancellationToken)
    {        
      var query = new GetCountQuery<TDto>()
      {
        Filters = $"{columnName}=={value}"             
      };
      int count = await mediator.Send(query, cancellationToken);
      return count == 0;
    }
  }
}
