using Events.WebAPI.Contract.Validation;

namespace Events.WebAPI.Util.Validation;

public class ValidationMessageProvider : IValidationMessageProvider
{
  public ValidationMessage UniqueSportName(string sportName)
    => new(ValidationErrorCodes.SportNameNotUnique, $"A sport named '{sportName}' already exists.");

  public ValidationMessage UniquePersonDocumentAndCountry()
    => new(ValidationErrorCodes.PersonDocumentCountryNotUnique, "A person with the same document number already exists for the selected country.");

  public ValidationMessage UniqueRegistration()
    => new(ValidationErrorCodes.RegistrationNotUnique, "The person is already registered for the selected sport at this event.");

  public ValidationMessage EventNotFound()
    => new(ValidationErrorCodes.EventNotFound, "The selected event does not exist.");

  public ValidationMessage PersonNotFound()
    => new(ValidationErrorCodes.PersonNotFound, "The selected person does not exist.");

  public ValidationMessage SportNotFound()
    => new(ValidationErrorCodes.SportNotFound, "The selected sport does not exist.");

  public ValidationMessage ForeignKeyNotFound(string propertyName)
    => new(ValidationErrorCodes.ForeignKeyNotFound, $"The selected value for {propertyName} does not exist.");
}
