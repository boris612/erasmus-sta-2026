using System.Globalization;
using Events.WebAPI.Contract.Services;
using Events.WebAPI.Contract.Services.Certificates;
using Events.WebAPI.Handlers.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Events.WebAPI.CertificateCreator.Pdf;

namespace Events.WebAPI.CertificateCreator;

public class RegistrationCertificateService : IRegistrationCertificateService
{
  private readonly EventsContext ctx;
  private readonly IHostEnvironment hostEnvironment;
  private readonly ILogger<RegistrationCertificateService> logger;
  private readonly CertificateCreatorPathsOptions pathsOptions;

  public RegistrationCertificateService(
    EventsContext ctx,
    IHostEnvironment hostEnvironment,
    IOptions<CertificateCreatorPathsOptions> pathsOptions,
    ILogger<RegistrationCertificateService> logger)
  {
    this.ctx = ctx;
    this.hostEnvironment = hostEnvironment;
    this.logger = logger;
    this.pathsOptions = pathsOptions.Value;
  }

  public async Task SynchronizeCertificateAsync(int eventId, int personId, CancellationToken cancellationToken)
  {
    var registrations = await ctx.Set<Registration>()
      .AsNoTracking()
      .Where(r => r.EventId == eventId && r.PersonId == personId)
      .OrderBy(r => r.Sport.Name)
      .Select(r => new CertificateRegistrationData
      {
        EventId = r.EventId,
        EventName = r.Event.Name,
        EventDate = r.Event.EventDate,
        PersonId = r.PersonId,
        FirstName = r.Person.FirstName,
        LastName = r.Person.LastName,
        FirstNameTranscription = r.Person.FirstNameTranscription,
        LastNameTranscription = r.Person.LastNameTranscription,
        SportName = r.Sport.Name
      })
      .ToListAsync(cancellationToken);

    string certificatePath = GetCertificatePath(eventId, personId);

    if (registrations.Count == 0)
    {
      DeleteCertificateIfExists(certificatePath);
      return;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(certificatePath)!);

    byte[] pdfBytes = CertificatePdfDocumentWriter.CreateCertificate(BuildModel(registrations));
    await File.WriteAllBytesAsync(certificatePath, pdfBytes, cancellationToken);

    logger.LogInformation(
      "Registration certificate generated for event #{EventId}, person #{PersonId} at {Path}",
      eventId,
      personId,
      certificatePath);
  }

  public Task<GeneratedFileReference?> GetCertificateAsync(int eventId, int personId, CancellationToken cancellationToken)
  {
    string certificatePath = GetCertificatePath(eventId, personId);
    if (!File.Exists(certificatePath))
      return Task.FromResult<GeneratedFileReference?>(null);

    return Task.FromResult<GeneratedFileReference?>(new GeneratedFileReference
    {
      FileName = Path.GetFileName(certificatePath),
      PhysicalPath = certificatePath
    });
  }

  private string GetCertificatePath(int eventId, int personId)
  {
    string rootPath = GetCertificatesRootPath();

    return Path.Combine(rootPath, eventId.ToString(CultureInfo.InvariantCulture), $"{eventId}-{personId}.pdf");
  }

  private string GetCertificatesRootPath()
  {
    return Path.IsPathRooted(pathsOptions.Certificates)
      ? pathsOptions.Certificates
      : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, pathsOptions.Certificates));
  }

  private void DeleteCertificateIfExists(string certificatePath)
  {
    DeleteFileIfExists(certificatePath, "Registration certificate deleted at {Path}");

    string? directory = Path.GetDirectoryName(certificatePath);
    if (!string.IsNullOrWhiteSpace(directory) &&
        Directory.Exists(directory) &&
        !Directory.EnumerateFileSystemEntries(directory).Any())
    {
      Directory.Delete(directory);
    }

  }

  private void DeleteFileIfExists(string path, string logMessage)
  {
    if (!File.Exists(path))
      return;

    File.Delete(path);
    logger.LogInformation(logMessage, path);
  }

  private static CertificatePdfModel BuildModel(IReadOnlyList<CertificateRegistrationData> registrations)
  {
    CertificateRegistrationData first = registrations[0];
    string originalFullName = $"{first.FirstName} {first.LastName}".Trim();
    string transcriptionFullName = $"{first.FirstNameTranscription} {first.LastNameTranscription}".Trim();

    var sports = registrations
      .Select(r => r.SportName)
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
      .ToList();

    return new CertificatePdfModel
    {
      Title = "Certificate of participation",
      PersonFullName = originalFullName,
      PersonFullNameTranscription = transcriptionFullName,
      EventName = first.EventName,
      EventDate = first.EventDate,
      EventId = first.EventId,
      PersonId = first.PersonId,
      SportNames = sports
    };
  }

  private sealed class CertificateRegistrationData
  {
    public int EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public DateOnly EventDate { get; init; }
    public int PersonId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FirstNameTranscription { get; init; } = string.Empty;
    public string LastNameTranscription { get; init; } = string.Empty;
    public string SportName { get; init; } = string.Empty;
  }
}
