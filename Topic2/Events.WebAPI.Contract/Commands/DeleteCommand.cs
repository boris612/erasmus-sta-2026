using MediatR;

namespace MobilityOne.Common.Commands;

public class DeleteCommand<TDto, TPK>(TPK id) : IRequest
{
  public TPK Id { get; set; } = id;
}
