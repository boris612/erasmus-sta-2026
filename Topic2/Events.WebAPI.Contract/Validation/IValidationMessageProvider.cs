namespace Events.WebAPI.Contract.Validation;

public interface IValidationMessageProvider
{
  ValidationMessage UniqueSportName(string sportName);
  ValidationMessage UniquePersonDocumentAndCountry();
  ValidationMessage UniqueRegistration();
  ValidationMessage EventNotFound();
  ValidationMessage PersonNotFound();
  ValidationMessage SportNotFound();
  ValidationMessage ForeignKeyNotFound(string propertyName);
}
