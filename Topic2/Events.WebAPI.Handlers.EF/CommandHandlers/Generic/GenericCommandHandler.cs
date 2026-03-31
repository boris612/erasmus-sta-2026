using MobilityOne.Common.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using AutoMapper;

namespace Events.WebAPI.Handlers.EF.CommandHandlers.Generic;

public class GenericCommandHandler<TDal, TDto, TPK> : IRequestHandler<AddCommand<TDto, TPK>, TPK>,
                                                      IRequestHandler<UpdateCommand<TDto>>,
                                                      IRequestHandler<DeleteCommand<TDto, TPK>>
        where TDal: class, IHasIdAsPK<TPK>
        where TDto: IHasIdAsPK<TPK>
        where TPK : IEquatable<TPK>
{
  protected DbContext Ctx { get; }
  protected ILogger Logger { get; }
  protected IMapper Mapper { get; }

  protected GenericCommandHandler(DbContext ctx, ILogger logger, IMapper mapper)
  {
    Ctx = ctx;
    Logger = logger;
    Mapper = mapper;
  }

  public virtual async Task<TPK> Handle(AddCommand<TDto, TPK> request, CancellationToken cancellationToken)
  {      
    var entity = Mapper.Map<TDto, TDal>(request.Dto);     
    Ctx.Add(entity);      
    await Ctx.SaveChangesAsync(cancellationToken);
    return entity.Id;
  }

  public virtual async Task Handle(UpdateCommand<TDto> request, CancellationToken cancellationToken)
  {
    var entity = await Ctx.Set<TDal>().FindAsync(request.Dto.Id);
    if (entity != null)
    {
      Mapper.Map(request.Dto, entity);
      await Ctx.SaveChangesAsync(cancellationToken);
    }
    else
    {
      Logger.LogError($"UpdateCommand<{typeof(TDto).Name}> : Invalid id #{request.Dto.Id}");
      throw new ArgumentException($"Invalid id: {request.Dto.Id}");
    }
  }

  public virtual async Task Handle(DeleteCommand<TDto, TPK> request, CancellationToken cancellationToken)
  {
    await Ctx.Set<TDal>().Where(d => d.Id.Equals(request.Id)).ExecuteDeleteAsync(cancellationToken);      
  }    
}
