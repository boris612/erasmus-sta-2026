using AutoMapper;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.CommandHandlers.Generic;
using Events.WebAPI.Handlers.EF.Models;
using Microsoft.Extensions.Logging;

namespace Events.WebAPI.Handlers.EF.CommandHandlers;

public class EventsCommandsHandler : GenericCommandHandler<Event, EventDTO, int>
{
  public EventsCommandsHandler(EventsContext ctx, ILogger<EventsCommandsHandler> logger, IMapper mapper)
    : base(ctx, logger, mapper)
  {
  }
}
