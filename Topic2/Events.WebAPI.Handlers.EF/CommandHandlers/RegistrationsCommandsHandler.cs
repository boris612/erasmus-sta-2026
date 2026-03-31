using AutoMapper;
using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.Messages;
using Events.WebAPI.Handlers.EF.CommandHandlers.Generic;
using Events.WebAPI.Handlers.EF.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MobilityOne.Common.Commands;

namespace Events.WebAPI.Handlers.EF.CommandHandlers;

public class RegistrationsCommandsHandler : GenericCommandHandler<Registration, RegistrationDTO, int>
{
  private readonly IPublishEndpoint publishEndpoint;

  public RegistrationsCommandsHandler(
    EventsContext ctx,
    ILogger<RegistrationsCommandsHandler> logger,
    IMapper mapper,
    IPublishEndpoint publishEndpoint)
    : base(ctx, logger, mapper)
  {
    this.publishEndpoint = publishEndpoint;
  }

  public override async Task<int> Handle(AddCommand<RegistrationDTO, int> request, CancellationToken cancellationToken)
  {
    int id = await base.Handle(request, cancellationToken);

    await publishEndpoint.Publish(new RegistrationCreated
    {
      RegistrationId = id,
      PersonId = request.Dto.PersonId,
      EventId = request.Dto.EventId,
      SportId = request.Dto.SportId
    }, cancellationToken);

    return id;
  }

  public override async Task Handle(UpdateCommand<RegistrationDTO> request, CancellationToken cancellationToken)
  {
    var entity = await Ctx.Set<Registration>().SingleOrDefaultAsync(r => r.Id == request.Dto.Id, cancellationToken);
    if (entity == null)
    {
      Logger.LogError("UpdateCommand<{DtoName}> : Invalid id #{Id}", typeof(RegistrationDTO).Name, request.Dto.Id);
      throw new ArgumentException($"Invalid id: {request.Dto.Id}");
    }

    int previousPersonId = entity.PersonId;
    int previousEventId = entity.EventId;
    int previousSportId = entity.SportId;

    await base.Handle(request, cancellationToken);

    await publishEndpoint.Publish(new RegistrationUpdated
    {
      RegistrationId = request.Dto.Id,
      PersonId = request.Dto.PersonId,
      EventId = request.Dto.EventId,
      SportId = request.Dto.SportId,
      PreviousPersonId = previousPersonId,
      PreviousEventId = previousEventId,
      PreviousSportId = previousSportId
    }, cancellationToken);
  }

  public override async Task Handle(DeleteCommand<RegistrationDTO, int> request, CancellationToken cancellationToken)
  {
    var entity = await Ctx.Set<Registration>()
      .AsNoTracking()
      .SingleOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

    if (entity == null)
    {
      Logger.LogError("DeleteCommand<{DtoName}> : Invalid id #{Id}", typeof(RegistrationDTO).Name, request.Id);
      throw new ArgumentException($"Invalid id: {request.Id}");
    }

    await base.Handle(request, cancellationToken);

    await publishEndpoint.Publish(new RegistrationDeleted
    {
      RegistrationId = entity.Id,
      PersonId = entity.PersonId,
      EventId = entity.EventId,
      SportId = entity.SportId
    }, cancellationToken);
  }
}
