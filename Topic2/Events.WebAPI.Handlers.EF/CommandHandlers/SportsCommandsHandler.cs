using AutoMapper;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.CommandHandlers.Generic;
using Events.WebAPI.Handlers.EF.Models;
using Microsoft.Extensions.Logging;

namespace Events.WebAPI.Handlers.EF.CommandHandlers
{
  public class SportsCommandsHandler : GenericCommandHandler<Sport, SportDTO, int>
  {
    public SportsCommandsHandler(EventsContext ctx, ILogger<SportsCommandsHandler> logger, IMapper mapper) 
      : base(ctx, logger, mapper)
    {
    }
  }
}
