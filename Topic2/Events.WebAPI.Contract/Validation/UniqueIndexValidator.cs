using System.Linq.Expressions;
using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.Queries.Generic;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Events.WebAPI.Contract.Validation;

public class UniqueIndexValidator<TDto, TPK> where TDto : IHasIdAsPK<TPK>
                                             where TPK : IEquatable<TPK>
{
  private readonly IMediator mediator;
  private readonly Expression<Func<TDto, object?>>[] selectors;
  private readonly Func<IReadOnlyList<string>, IReadOnlyList<string>, ValidationMessage>? errorMessageFactory;

  public UniqueIndexValidator(
    IMediator mediator,
    Func<IReadOnlyList<string>, IReadOnlyList<string>, ValidationMessage>? errorMessageFactory = null,
    params Expression<Func<TDto, object?>>[] selectors)
  {
    this.mediator = mediator;
    this.errorMessageFactory = errorMessageFactory;
    this.selectors = selectors;
  }

  public async Task Validate(string value, ValidationContext<AddCommand<TDto, TPK>> context, CancellationToken cancellationToken)
  {
    string columnName = GetColumnName(context);

    var query = new GetCountQuery<TDto>
    {
      Filters = $"{columnName}==*{EscapeFilterValue(value)}"
    };

    int count = await mediator.Send(query, cancellationToken);
    if (count > 0)
    {
      ValidationMessage validationMessage = BuildErrorMessage([columnName], [value]);
      context.AddFailure(new ValidationFailure(columnName, validationMessage.Message) { ErrorCode = validationMessage.Code });
    }
  }

  public async Task Validate(TDto dto, ValidationContext<AddCommand<TDto, TPK>> context, CancellationToken cancellationToken)
  {
    var query = new GetCountQuery<TDto>();

    List<string> columnNames = [];
    List<string> values = [];
    List<string> filters = [];
    foreach (var selector in selectors)
    {
      string columnName = GetColumnName(selector);
      columnNames.Add(columnName);
      object? rawValue = selector.Compile().Invoke(dto);
      string value = FormatValue(rawValue);
      values.Add(value);
      filters.Add(BuildEqualsFilter(columnName, rawValue));
    }

    query.Filters = string.Join(",", filters);

    int count = await mediator.Send(query, cancellationToken);
    if (count > 0)
    {
      ValidationMessage validationMessage = BuildErrorMessage(columnNames, values);
      context.AddFailure(new ValidationFailure(context.PropertyPath, validationMessage.Message) { ErrorCode = validationMessage.Code });
    }
  }

  public async Task ValidateExisting(string value, ValidationContext<UpdateCommand<TDto>> context, CancellationToken cancellationToken)
  {
    string columnName = GetColumnName(context);

    UpdateCommand<TDto> validatingObject = context.InstanceToValidate;
    var query = new GetItemsQuery<TDto>
    {
      Filters = $"{columnName}==*{EscapeFilterValue(value)}",
      Page = 1,
      PageSize = 2
    };

    List<TDto> items = await mediator.Send(query, cancellationToken);
    if (items.Count > 0)
    {
      bool valueBelongsToValidatingItem = items.Any(item => item.Id.Equals(validatingObject.Dto.Id));
      if (!valueBelongsToValidatingItem)
      {
        ValidationMessage validationMessage = BuildErrorMessage([columnName], [value]);
        context.AddFailure(new ValidationFailure(columnName, validationMessage.Message) { ErrorCode = validationMessage.Code });
      }
    }
  }

  public async Task ValidateExisting(TDto dto, ValidationContext<UpdateCommand<TDto>> context, CancellationToken cancellationToken)
  {
    var query = new GetItemsQuery<TDto>();

    List<string> columnNames = [];
    List<string> values = [];
    List<string> filters = [];
    foreach (var selector in selectors)
    {
      string columnName = GetColumnName(selector);
      columnNames.Add(columnName);
      object? rawValue = selector.Compile().Invoke(dto);
      string value = FormatValue(rawValue);
      values.Add(value);
      filters.Add(BuildEqualsFilter(columnName, rawValue));
    }

    query.Filters = string.Join(",", filters);
    query.Page = 1;
    query.PageSize = 2;

    List<TDto> items = await mediator.Send(query, cancellationToken);
    if (items.Count > 0)
    {
      bool valueBelongsToValidatingItem = items.Any(item => item.Id.Equals(dto.Id));
      if (!valueBelongsToValidatingItem)
      {
        ValidationMessage validationMessage = BuildErrorMessage(columnNames, values);
        context.AddFailure(new ValidationFailure(context.PropertyPath, validationMessage.Message) { ErrorCode = validationMessage.Code });
      }
    }
  }

  private ValidationMessage BuildErrorMessage(IReadOnlyList<string> columnNames, IReadOnlyList<string> values)
  {
    if (errorMessageFactory != null)
      return errorMessageFactory(columnNames, values);

    return columnNames.Count == 1
      ? new ValidationMessage(ValidationErrorCodes.UniqueConstraintViolation, $"{columnNames[0]} must be unique. Value {values[0]} has been already used!")
      : new ValidationMessage(ValidationErrorCodes.UniqueConstraintViolation, $"n-tuple ({string.Join(", ", columnNames)}) = ({string.Join(", ", values)}) must be unique.");
  }

  private string GetColumnName(Expression<Func<TDto, object?>> expression)
  {
    Expression body = expression.Body;
    if (body is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Convert)
      body = unaryExpression.Operand;

    if (body is MemberExpression memberExpression)
      return memberExpression.Member.Name;

    if (body is MethodCallExpression methodCallExpression && methodCallExpression.Object is MemberExpression objectMemberExpression)
      return objectMemberExpression.Member.Name;

    throw new Exception($"Invalid nodetype ({body.NodeType}) in expression");
  }

  private string GetColumnName<T>(ValidationContext<T> context)
  {
    if (selectors.Length != 1)
      throw new Exception($"Unique index contains several columns, and must not be called on a single property {context.PropertyPath}");

    Expression body = selectors[0].Body;
    if (body is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Convert)
      body = unaryExpression.Operand;

    if (body is not MemberExpression memberExpression)
      throw new Exception($"Invalid nodetype ({body.NodeType}) in expression");

    string columnName = memberExpression.Member.Name;
    if (columnName != context.PropertyPath.Replace(nameof(UpdateCommand<TDto>.Dto) + ".", ""))
      throw new Exception($"Unique index is defined on {columnName} but called on {context.PropertyPath}");

    return columnName;
  }

  private static string EscapeFilterValue(string value)
  {
    string escaped = value
      .Replace("\\", "\\\\")
      .Replace(",", "\\,")
      .Replace("|", "\\|");

    return string.Equals(escaped, "null", StringComparison.Ordinal)
      ? "\\null"
      : escaped;
  }

  private static string BuildEqualsFilter(string columnName, object? value)
  {
    string formattedValue = FormatValue(value);
    return value is string
      ? $"{columnName}==*{EscapeFilterValue(formattedValue)}"
      : $"{columnName}=={formattedValue}";
  }

  private static string FormatValue(object? value)
  {
    return value switch
    {
      null => "null",
      string stringValue => stringValue,
      DateOnly dateOnlyValue => dateOnlyValue.ToString("yyyy-MM-dd"),
      DateTime dateTimeValue => dateTimeValue.ToString("O"),
      bool boolValue => boolValue ? "true" : "false",
      IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
      _ => value.ToString() ?? string.Empty
    };
  }
}
