using FluentValidation;
using MediatR;

namespace Events.WebAPI.Contract.Validation;

public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IBaseRequest
{
  private readonly IEnumerable<IValidator<TRequest>> validators;

  public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
  {
    this.validators = validators;
  }

  public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
  {
    if (validators.Any())
    {
      var context = new ValidationContext<TRequest>(request);
      var validationResults = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
      var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();
      if (failures.Count != 0)
        throw new ValidationException(failures);
    }
    return await next();
  }
}
