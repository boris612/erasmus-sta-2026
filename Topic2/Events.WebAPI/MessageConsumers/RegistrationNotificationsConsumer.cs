using Events.WebAPI.Contract.Services.Certificates;
using Events.WebAPI.Contract.Messages;
using MassTransit;

namespace Events.WebAPI.MessageConsumers;

public class RegistrationNotificationsConsumer :
  IConsumer<RegistrationCreated>,
  IConsumer<RegistrationUpdated>,
  IConsumer<RegistrationDeleted>
{
  private readonly IRegistrationCertificateService documentsService;

  public RegistrationNotificationsConsumer(IRegistrationCertificateService documentsService)
  {
    this.documentsService = documentsService;
  }

  public Task Consume(ConsumeContext<RegistrationCreated> context)
  {
    return documentsService.SynchronizeCertificateAsync(
      context.Message.EventId,
      context.Message.PersonId,
      context.CancellationToken);
  }

  public async Task Consume(ConsumeContext<RegistrationUpdated> context)
  {
    await documentsService.SynchronizeCertificateAsync(
      context.Message.EventId,
      context.Message.PersonId,
      context.CancellationToken);

    if (context.Message.PreviousEventId != context.Message.EventId ||
        context.Message.PreviousPersonId != context.Message.PersonId)
    {
      await documentsService.SynchronizeCertificateAsync(
        context.Message.PreviousEventId,
        context.Message.PreviousPersonId,
        context.CancellationToken);
    }
  }

  public Task Consume(ConsumeContext<RegistrationDeleted> context)
  {
    return documentsService.SynchronizeCertificateAsync(
      context.Message.EventId,
      context.Message.PersonId,
      context.CancellationToken);
  }
}
