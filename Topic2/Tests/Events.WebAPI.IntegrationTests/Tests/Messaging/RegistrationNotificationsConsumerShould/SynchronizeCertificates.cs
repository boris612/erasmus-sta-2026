using Events.WebAPI.Contract.Messages;
using Events.WebAPI.Contract.Services.Certificates;
using Events.WebAPI.MessageConsumers;
using MassTransit;
using Moq;

namespace Tests.Messaging.RegistrationNotificationsConsumerShould;

public class SynchronizeCertificates
{
  [Fact]
  public async Task SynchronizeCurrentCertificateForCreatedMessage()
  {
    var service = new Mock<IRegistrationCertificateService>();
    service
      .Setup(x => x.SynchronizeCertificateAsync(100, 200, It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var consumer = new RegistrationNotificationsConsumer(service.Object);
    var context = CreateContext(new RegistrationCreated
    {
      RegistrationId = 1,
      EventId = 100,
      PersonId = 200,
      SportId = 300
    });

    await consumer.Consume(context.Object);

    service.Verify(x => x.SynchronizeCertificateAsync(100, 200, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SynchronizeCurrentAndPreviousCertificatesForUpdatedMessageWhenPersonOrEventChanges()
  {
    var service = new Mock<IRegistrationCertificateService>();
    service
      .Setup(x => x.SynchronizeCertificateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var consumer = new RegistrationNotificationsConsumer(service.Object);
    var context = CreateContext(new RegistrationUpdated
    {
      RegistrationId = 1,
      EventId = 101,
      PersonId = 201,
      SportId = 301,
      PreviousEventId = 100,
      PreviousPersonId = 200,
      PreviousSportId = 300
    });

    await consumer.Consume(context.Object);

    service.Verify(x => x.SynchronizeCertificateAsync(101, 201, It.IsAny<CancellationToken>()), Times.Once);
    service.Verify(x => x.SynchronizeCertificateAsync(100, 200, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task SynchronizeCurrentCertificateForDeletedMessage()
  {
    var service = new Mock<IRegistrationCertificateService>();
    service
      .Setup(x => x.SynchronizeCertificateAsync(100, 200, It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var consumer = new RegistrationNotificationsConsumer(service.Object);
    var context = CreateContext(new RegistrationDeleted
    {
      RegistrationId = 1,
      EventId = 100,
      PersonId = 200,
      SportId = 300
    });

    await consumer.Consume(context.Object);

    service.Verify(x => x.SynchronizeCertificateAsync(100, 200, It.IsAny<CancellationToken>()), Times.Once);
  }

  private static Mock<ConsumeContext<TMessage>> CreateContext<TMessage>(TMessage message)
    where TMessage : class
  {
    var context = new Mock<ConsumeContext<TMessage>>();
    context.SetupGet(x => x.Message).Returns(message);
    context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);
    return context;
  }
}
