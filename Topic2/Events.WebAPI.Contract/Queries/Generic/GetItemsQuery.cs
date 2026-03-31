using MediatR;

namespace Events.WebAPI.Contract.Queries.Generic;

public class GetItemsQuery<TDto> : IRequest<List<TDto>>
{
  public string? Filters { get; set; }
  public string? Sort { get; set; }
  public bool Ascending { get; set; }
  public int? PageSize { get; set; }
  public int? Page { get; set; }
}
