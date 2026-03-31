using MediatR;

namespace Events.WebAPI.Contract.Command;

public class AddCommand<TDto, TPK>(TDto dto) : IRequest<TPK>
{
  public TDto Dto { get; set; } = dto;
}