using Events.WebAPI.Contract.Services.Certificates;
using Events.WebAPI.Contract.Services.EventRegistrationsExcel;
using Tests.ApplicationFactories.TestAuth;
using Events.WebAPI.MessageConsumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Tests.ApplicationFactories.Messaging;

public class MessagingWebApplicationFactory : TestAuthWebApplicationFactory
{
  public Mock<IRegistrationCertificateService> CertificateServiceMock { get; } = new();
  public Mock<IEventRegistrationsExcelService> ExcelServiceMock { get; } = new();

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    base.ConfigureWebHost(builder);

    builder.ConfigureServices(services =>
    {
      services.RemoveAll<IPublishEndpoint>();
      services.RemoveAll<IRegistrationCertificateService>();
      services.RemoveAll<IEventRegistrationsExcelService>();

      services.AddSingleton(CertificateServiceMock.Object);
      services.AddSingleton(ExcelServiceMock.Object);

      services.AddMassTransitTestHarness(cfg =>
      {
        cfg.AddConsumer<RegistrationNotificationsConsumer>();
        cfg.AddConsumer<EventRegistrationsExcelConsumer>();

        cfg.UsingInMemory((context, busCfg) =>
        {
          busCfg.ConfigureEndpoints(context);
        });
      });
    });
  }

  public void ResetServiceMocks()
  {
    CertificateServiceMock.Reset();
    CertificateServiceMock
      .Setup(x => x.SynchronizeCertificateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);
    CertificateServiceMock
      .Setup(x => x.GetCertificateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((Events.WebAPI.Contract.Services.GeneratedFileReference?)null);

    ExcelServiceMock.Reset();
    ExcelServiceMock
      .Setup(x => x.SynchronizeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);
    ExcelServiceMock
      .Setup(x => x.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((Events.WebAPI.Contract.Services.GeneratedFileReference?)null);
  }
}
