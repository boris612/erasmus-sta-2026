using Events.WebAPI.Contract.DTOs;
using MediatR;

namespace Events.WebAPI.Contract.LookupQueries;

public class LookupPeopleQuery : IRequest<List<IdName<int>>>
{
  public string? Text { get; set; }

  public string? CountryCode { get; set; }
}
