namespace Events.WebAPI.Contract.Validation;

public static class ValidationErrorCodes
{
  public const string UniqueConstraintViolation = "unique_constraint_violation";
  public const string SportNameNotUnique = "sport_name_not_unique";
  public const string PersonDocumentCountryNotUnique = "person_document_country_not_unique";
  public const string RegistrationNotUnique = "registration_not_unique";
  public const string ForeignKeyNotFound = "foreign_key_not_found";
  public const string EventNotFound = "event_not_found";
  public const string PersonNotFound = "person_not_found";
  public const string SportNotFound = "sport_not_found";
}
