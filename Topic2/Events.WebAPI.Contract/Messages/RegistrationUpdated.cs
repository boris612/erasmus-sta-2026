namespace Events.WebAPI.Contract.Messages;

public record RegistrationUpdated
{
  public int RegistrationId { get; init; }
  public int PersonId { get; init; }
  public int EventId { get; init; }
  public int SportId { get; init; }
  public int PreviousPersonId { get; init; }
  public int PreviousEventId { get; init; }
  public int PreviousSportId { get; init; }
}
