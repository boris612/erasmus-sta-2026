using Events.WebAPI.Contract.DTOs;
using MediatR;

namespace Events.WebAPI.Contract.LookupQueries;

public class LookupCountryQuery : IRequest<List<IdName<string>>>
{
  public string? Text { get; set; }
}
