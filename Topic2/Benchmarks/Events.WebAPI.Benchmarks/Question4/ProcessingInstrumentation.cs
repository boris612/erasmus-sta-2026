using System.Diagnostics;
using Events.WebAPI.CertificateCreator;
using Events.WebAPI.Contract.Services.Certificates;
using Events.WebAPI.Contract.Services.EventRegistrationsExcel;
using Events.WebAPI.ExcelExporter;

namespace Events.WebAPI.Benchmarks;

public sealed class ServiceCompletionMonitor
{
  private readonly object sync = new();
  private TaskCompletionSource taskCompletionSource = NewCompletionSource();
  private int remainingCertificates;
  private int remainingExcels;
  private TimeSpan certificateDuration;
  private TimeSpan excelDuration;

  public void BeginIteration(int expectedCertificates, int expectedExcels)
  {
    lock (sync)
    {
      remainingCertificates = expectedCertificates;
      remainingExcels = expectedExcels;
      certificateDuration = TimeSpan.Zero;
      excelDuration = TimeSpan.Zero;
      taskCompletionSource = NewCompletionSource();

      if (remainingCertificates == 0 && remainingExcels == 0)
        taskCompletionSource.TrySetResult();
    }
  }

  public void CertificateCompleted(TimeSpan duration)
  {
    lock (sync)
    {
      certificateDuration = duration;
      remainingCertificates--;
      TryComplete();
    }
  }

  public void ExcelCompleted(TimeSpan duration)
  {
    lock (sync)
    {
      excelDuration = duration;
      remainingExcels--;
      TryComplete();
    }
  }

  public async Task WaitForCompletionAsync(TimeSpan timeout)
  {
    using var timeoutTokenSource = new CancellationTokenSource(timeout);
    await taskCompletionSource.Task.WaitAsync(timeoutTokenSource.Token);
  }

  public ServiceCompletionSnapshot GetSnapshot()
  {
    lock (sync)
    {
      return new ServiceCompletionSnapshot(certificateDuration, excelDuration);
    }
  }

  private void TryComplete()
  {
    if (remainingCertificates <= 0 && remainingExcels <= 0)
      taskCompletionSource.TrySetResult();
  }

  private static TaskCompletionSource NewCompletionSource()
  {
    return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
  }
}

public sealed record ServiceCompletionSnapshot(TimeSpan CertificateDuration, TimeSpan ExcelDuration);

internal sealed class InstrumentedRegistrationCertificateService : IRegistrationCertificateService
{
  private readonly RegistrationCertificateService inner;
  private readonly ServiceCompletionMonitor monitor;

  public InstrumentedRegistrationCertificateService(
    RegistrationCertificateService inner,
    ServiceCompletionMonitor monitor)
  {
    this.inner = inner;
    this.monitor = monitor;
  }

  public async Task SynchronizeCertificateAsync(int eventId, int personId, CancellationToken cancellationToken)
  {
    Stopwatch stopwatch = Stopwatch.StartNew();
    await inner.SynchronizeCertificateAsync(eventId, personId, cancellationToken);
    stopwatch.Stop();
    monitor.CertificateCompleted(stopwatch.Elapsed);
  }

  public Task<Events.WebAPI.Contract.Services.GeneratedFileReference?> GetCertificateAsync(int eventId, int personId, CancellationToken cancellationToken)
  {
    return inner.GetCertificateAsync(eventId, personId, cancellationToken);
  }
}

internal sealed class InstrumentedEventRegistrationsExcelService : IEventRegistrationsExcelService
{
  private readonly EventRegistrationsExcelService inner;
  private readonly ServiceCompletionMonitor monitor;

  public InstrumentedEventRegistrationsExcelService(
    EventRegistrationsExcelService inner,
    ServiceCompletionMonitor monitor)
  {
    this.inner = inner;
    this.monitor = monitor;
  }

  public async Task SynchronizeAsync(int eventId, CancellationToken cancellationToken)
  {
    Stopwatch stopwatch = Stopwatch.StartNew();
    await inner.SynchronizeAsync(eventId, cancellationToken);
    stopwatch.Stop();
    monitor.ExcelCompleted(stopwatch.Elapsed);
  }

  public Task<Events.WebAPI.Contract.Services.GeneratedFileReference?> GetAsync(int eventId, CancellationToken cancellationToken)
  {
    return inner.GetAsync(eventId, cancellationToken);
  }
}
