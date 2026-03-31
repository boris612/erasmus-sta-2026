namespace Events.WebAPI.Contract.Services.Certificates;

public interface IRegistrationCertificateService
{
  Task SynchronizeCertificateAsync(int eventId, int personId, CancellationToken cancellationToken);
  Task<GeneratedFileReference?> GetCertificateAsync(int eventId, int personId, CancellationToken cancellationToken);
}
