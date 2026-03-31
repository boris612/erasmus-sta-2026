using MediatR;

namespace Events.WebAPI.Contract.Queries.Generic;

public abstract class GetCountQuery : IRequest<int>
{    
  public string? Filters { get; set; }   
}

public class GetCountQuery<TDto> : GetCountQuery
{    
  public static GetCountQuery<TDto> CreateForPK<TPK>(TPK id) where TPK : IEquatable<TPK> {
    var query = new GetCountQuery<TDto>()
    {
      Filters = $"id=={id}"
    };
    return query;
  }
}  
