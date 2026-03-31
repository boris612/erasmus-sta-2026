using AutoMapper;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.Models;
using Events.WebAPI.Handlers.EF.QueryHandlers.Generic;
using Microsoft.Extensions.Logging;
using Sieve.Services;

namespace Events.WebAPI.Handlers.EF.QueryHandlers;

public class PeopleQueryHandler : GenericQueryHandler<PersonDTO, Person, int>
{
  public PeopleQueryHandler(EventsContext ctx, ILogger<PeopleQueryHandler> logger, IMapper mapper, ISieveProcessor sieveProcessor)
    : base(ctx, logger, mapper, sieveProcessor)
  {
  }
}
