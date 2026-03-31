using MediatR;

namespace Events.WebAPI.Contract.Command;

public class UpdateCommand<TDto>(TDto dto) : IRequest
{     
  public TDto Dto { get; set; } = dto;
}
