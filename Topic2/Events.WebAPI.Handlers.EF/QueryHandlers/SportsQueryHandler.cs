using AutoMapper;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.Models;
using Events.WebAPI.Handlers.EF.QueryHandlers.Generic;
using Microsoft.Extensions.Logging;
using Sieve.Services;

namespace Events.WebAPI.Handlers.EF.QueryHandlers;

public class SportsQueryHandler : GenericQueryHandler<SportDTO, Sport, int>
{
  public SportsQueryHandler(EventsContext ctx, ILogger<SportsQueryHandler> logger, IMapper mapper, ISieveProcessor sieveProcessor) 
    : base(ctx, logger, mapper, sieveProcessor)
  {

  } 
}
