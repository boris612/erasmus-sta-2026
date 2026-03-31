using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.LookupQueries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Events.WebAPI.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class LookupController : ControllerBase
{
  [HttpGet]
  public async Task<ActionResult<List<IdName<string>>>> Countries(string? text, [FromServices] IMediator mediator)
  {
    var countries = await mediator.Send(new LookupCountryQuery { Text = text });
    return countries;
  }

  [Authorize(Policy = nameof(Policies.ReadData))]
  [HttpGet]
  public async Task<ActionResult<List<IdName<int>>>> People(string? text, string? countryCode, [FromServices] IMediator mediator)
  {
    var people = await mediator.Send(new LookupPeopleQuery
    {
      Text = text,
      CountryCode = countryCode
    });
    return people;
  }
}
