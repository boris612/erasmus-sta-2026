namespace Events.WebAPI.Contract.Services.EventRegistrationsExcel;

public interface IEventRegistrationsExcelService
{
  Task SynchronizeAsync(int eventId, CancellationToken cancellationToken);
  Task<GeneratedFileReference?> GetAsync(int eventId, CancellationToken cancellationToken);
}
