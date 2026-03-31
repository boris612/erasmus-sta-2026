using Events.WebAPI.Contract.DTOs;
using MediatR;

namespace Events.WebAPI.Contract.Queries.Generic;

public class DoesItemExistsQuery<TDto, TPK> (TPK id) : IRequest<bool>, IHasIdAsPK<TPK>
  where TPK : IEquatable<TPK>
{
  public TPK Id { get; set; } = id;
}
