using Events.WebAPI.Contract.Services;
using Events.WebAPI.Contract.Services.EventRegistrationsExcel;
using Events.WebAPI.Handlers.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Events.WebAPI.ExcelExporter;

public class EventRegistrationsExcelService : IEventRegistrationsExcelService
{
  private readonly EventsContext ctx;
  private readonly IHostEnvironment hostEnvironment;
  private readonly ILogger<EventRegistrationsExcelService> logger;
  private readonly ExcelExporterPathsOptions pathsOptions;

  public EventRegistrationsExcelService(
    EventsContext ctx,
    IHostEnvironment hostEnvironment,
    IOptions<ExcelExporterPathsOptions> pathsOptions,
    ILogger<EventRegistrationsExcelService> logger)
  {
    this.ctx = ctx;
    this.hostEnvironment = hostEnvironment;
    this.logger = logger;
    this.pathsOptions = pathsOptions.Value;
  }

  public async Task SynchronizeAsync(int eventId, CancellationToken cancellationToken)
  {
    var registrations = ctx.Set<Registration>()
      .AsNoTracking()
      .Where(r => r.EventId == eventId)
      .OrderBy(r => r.Person.LastName)
      .ThenBy(r => r.Person.FirstName)
      .ThenBy(r => r.Sport.Name)
      .Select(r => new EventRegistrationsExcelWriter.RowData
      {
        RegistrationId = r.Id,
        RegisteredAt = r.RegisteredAt,
        PersonId = r.PersonId,
        FirstName = r.Person.FirstName,
        LastName = r.Person.LastName,
        FirstNameTranscription = r.Person.FirstNameTranscription,
        LastNameTranscription = r.Person.LastNameTranscription,
        CountryName = r.Person.CountryCodeNavigation.Name,
        SportName = r.Sport.Name
      });

    string excelPath = GetPath(eventId);
    EventRegistrationsExcelWriter.RowData? firstRow = await registrations.FirstOrDefaultAsync(cancellationToken);
    if (firstRow == null)
    {
      DeleteFileIfExists(excelPath);
      return;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(excelPath)!);
    await EventRegistrationsExcelWriter.WriteAsync(excelPath, registrations, firstRow, cancellationToken);

    logger.LogInformation(
      "Event registrations Excel generated for event #{EventId} at {Path}",
      eventId,
      excelPath);
  }

  public Task<GeneratedFileReference?> GetAsync(int eventId, CancellationToken cancellationToken)
  {
    string excelPath = GetPath(eventId);
    if (!File.Exists(excelPath))
      return Task.FromResult<GeneratedFileReference?>(null);

    return Task.FromResult<GeneratedFileReference?>(new GeneratedFileReference
    {
      FileName = Path.GetFileName(excelPath),
      PhysicalPath = excelPath
    });
  }

  private string GetPath(int eventId)
  {
    string rootPath = Path.IsPathRooted(pathsOptions.Certificates)
      ? pathsOptions.Certificates
      : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, pathsOptions.Certificates));

    return Path.Combine(rootPath, $"{eventId}.xlsx");
  }

  private void DeleteFileIfExists(string path)
  {
    if (!File.Exists(path))
      return;

    File.Delete(path);
    logger.LogInformation("Event registrations Excel deleted at {Path}", path);
  }
}
