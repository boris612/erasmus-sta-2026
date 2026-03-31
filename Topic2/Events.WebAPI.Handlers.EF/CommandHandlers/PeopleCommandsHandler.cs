using AutoMapper;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.CommandHandlers.Generic;
using Events.WebAPI.Handlers.EF.Models;
using Microsoft.Extensions.Logging;

namespace Events.WebAPI.Handlers.EF.CommandHandlers;

public class PeopleCommandsHandler : GenericCommandHandler<Person, PersonDTO, int>
{
  public PeopleCommandsHandler(EventsContext ctx, ILogger<PeopleCommandsHandler> logger, IMapper mapper)
    : base(ctx, logger, mapper)
  {
  }
}
