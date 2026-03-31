using Events.WebAPI.Contract.DTOs;
using MediatR;

namespace Events.WebAPI.Contract.Queries.Generic;

public class GetSingleItemQuery<TDto, TPK>(TPK id) : IRequest<TDto>, IHasIdAsPK<TPK>
  where TPK : IEquatable<TPK>
{
  public TPK Id { get; set; } = id;    
}
