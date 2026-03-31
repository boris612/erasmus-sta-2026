using Events.WebAPI.Contract.Services.EventRegistrationsExcel;
using Events.WebAPI.Contract.Messages;
using MassTransit;

namespace Events.WebAPI.MessageConsumers;

public class EventRegistrationsExcelConsumer :
  IConsumer<RegistrationCreated>,
  IConsumer<RegistrationUpdated>,
  IConsumer<RegistrationDeleted>
{
  private readonly IEventRegistrationsExcelService excelService;

  public EventRegistrationsExcelConsumer(IEventRegistrationsExcelService excelService)
  {
    this.excelService = excelService;
  }

  public Task Consume(ConsumeContext<RegistrationCreated> context)
  {
    return excelService.SynchronizeAsync(
      context.Message.EventId,
      context.CancellationToken);
  }

  public async Task Consume(ConsumeContext<RegistrationUpdated> context)
  {
    await excelService.SynchronizeAsync(
      context.Message.EventId,
      context.CancellationToken);

    if (context.Message.PreviousEventId != context.Message.EventId)
    {
      await excelService.SynchronizeAsync(
        context.Message.PreviousEventId,
        context.CancellationToken);
    }
  }

  public Task Consume(ConsumeContext<RegistrationDeleted> context)
  {
    return excelService.SynchronizeAsync(
      context.Message.EventId,
      context.CancellationToken);
  }
}
