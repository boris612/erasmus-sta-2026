using AutoMapper;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.Models;
using Events.WebAPI.Handlers.EF.QueryHandlers.Generic;
using Microsoft.Extensions.Logging;
using Sieve.Services;

namespace Events.WebAPI.Handlers.EF.QueryHandlers;

public class EventsQueryHandler : GenericQueryHandler<EventDTO, Event, int>
{
  public EventsQueryHandler(EventsContext ctx, ILogger<EventsQueryHandler> logger, IMapper mapper, ISieveProcessor sieveProcessor)
    : base(ctx, logger, mapper, sieveProcessor)
  {
  }
}
