namespace Events.WebAPI.Contract.Messages;

public record RegistrationCreated
{
  public int RegistrationId { get; init; }
  public int PersonId { get; init; }
  public int EventId { get; init; }
  public int SportId { get; init; }
}
