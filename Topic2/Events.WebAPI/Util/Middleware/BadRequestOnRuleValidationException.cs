using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Events.WebAPI.Util.Extensions;
using Events.WebAPI.Contract.Command;

namespace Events.WebAPI.Util.Middleware;

public class BadRequestOnRuleValidationException : ExceptionFilterAttribute
{
  private readonly ILogger<BadRequestOnRuleValidationException> logger;

  public BadRequestOnRuleValidationException(ILogger<BadRequestOnRuleValidationException> logger)
  {
    this.logger = logger;
  }

  public override void OnException(ExceptionContext context)
  {
    if (context.Exception is ValidationException)
    {
      
      string exceptionMessage = context.Exception.CompleteExceptionMessage();        
      logger.LogDebug("Validation error: {0}", exceptionMessage);
      
      ValidationException exc = (ValidationException)context.Exception;
      Dictionary<string, List<string>> validationErrors = new Dictionary<string, List<string>>();
      Dictionary<string, List<string>> validationErrorCodes = new Dictionary<string, List<string>>();

      foreach(var failure in exc.Errors)
      {
        //remove prefix Dto. (part of Update and AddCommand)
        string propertyName = failure.PropertyName.Replace(nameof(AddCommand<object, object>.Dto) + ".", "");
        if (propertyName == nameof(AddCommand<object, object>.Dto))
        {
          propertyName = string.Empty;
        }

        validationErrors.GetOrCreate(propertyName).Add(failure.ErrorMessage);

        if (!string.IsNullOrWhiteSpace(failure.ErrorCode))
        {
          validationErrorCodes.GetOrCreate(propertyName).Add(failure.ErrorCode);
        }
      }

      var problemDetails = new ValidationProblemDetails(validationErrors.ToDictionary(d => d.Key, d => d.Value.ToArray()))
      {
        Detail = context.Exception.Message,
        Title = "Validation exception",
        Instance = context.HttpContext.TraceIdentifier
      };
      if (validationErrorCodes.Count > 0)
      {
        problemDetails.Extensions["errorCodes"] = validationErrorCodes.ToDictionary(d => d.Key, d => d.Value.ToArray());
      }
      context.Result = new ObjectResult(problemDetails)
      {
        ContentTypes = { "application/problem+json" },
        StatusCode = StatusCodes.Status400BadRequest
      };

      context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
      context.ExceptionHandled = true;

    }     
  }   
}
