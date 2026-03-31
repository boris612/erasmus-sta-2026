namespace Events.WebAPI.CertificateCreator.Pdf;

internal sealed class CertificatePdfModel
{
  public string Title { get; init; } = string.Empty;
  public string PersonFullName { get; init; } = string.Empty;
  public string PersonFullNameTranscription { get; init; } = string.Empty;
  public string EventName { get; init; } = string.Empty;
  public DateOnly EventDate { get; init; }
  public int EventId { get; init; }
  public int PersonId { get; init; }
  public IReadOnlyList<string> SportNames { get; init; } = Array.Empty<string>();
}
