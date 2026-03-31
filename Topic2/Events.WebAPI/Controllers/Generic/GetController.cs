using AutoMapper;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.Queries.Generic;
using Events.WebAPI.Models;
using Events.WebAPI.Util.Middleware;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Events.WebAPI.Controllers.Generic;

[Authorize(Policy = nameof(Policies.ReadData))]
[ApiController]
[Route("[controller]")]
[TypeFilter(typeof(BadRequestOnRuleValidationException), Order = 20)]
[TypeFilter(typeof(ProblemDetailsForSqlException), Order = 10)]
[TypeFilter(typeof(ProblemDetailsForException), Order = 1)] //last one
public abstract class GetController<TDto, TPK> : ControllerBase
   where TDto : IHasIdAsPK<TPK>
   where TPK : IEquatable<TPK>
{

  /// <summary>
  ///  Get number of item satisfying filters
  /// </summary>
  /// <param name="filters">Each filter is like  key(operator)value, using Sieve syntax</param>  
  /// <param name="mediator"></param>
  /// <param name="mapper"></param>
  /// <returns></returns>
  [HttpGet(nameof(Count))]
  public virtual async Task<int> Count(string filters, [FromServices] IMediator mediator, [FromServices] IMapper mapper)
  {
    var countRequest = new GetCountQuery<TDto>
    {
      Filters = filters,    
    };
    int count = await mediator.Send(countRequest);
    return count;
  }

  /// <summary>
  /// Returns single item based on primary key value
  /// </summary>
  /// <param name="id"></param>
  /// <param name="mediator"></param>
  /// <returns></returns>
  [HttpGet("{id}")]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public virtual async Task<ActionResult<TDto>> Get(TPK id, [FromServices] IMediator mediator)
  {
    var query = new GetSingleItemQuery<TDto, TPK>(id);
    var item = await mediator.Send(query);
    return item != null ?  item : Problem(statusCode: StatusCodes.Status404NotFound, detail: $"No data for id = {id}");
  }

  /// <summary>
  ///  Get all items based on (lazy) load parameters (paging, sorting, and filtering)
  /// </summary>
  /// <param name="loadParams"></param>
  /// <param name="mediator"></param>
  /// <param name="mapper"></param>
  /// <returns></returns>
  [HttpGet]
  public virtual async Task<Items<TDto>> GetAll([FromQuery] LoadParams loadParams, [FromServices] IMediator mediator, [FromServices] IMapper mapper)
  {
    loadParams ??= new();
    var result = new Items<TDto>();
    var countRequest = new GetCountQuery<TDto>
    {
      Filters = loadParams.Filters
    };
    result.Count = await mediator.Send(countRequest);

    if (result.Count > 0)
    {
      var dataRequest = new GetItemsQuery<TDto>
      {        
        Filters = loadParams.Filters,
        Sort = loadParams.Sort,
        Page = loadParams.Page,
        PageSize = loadParams.PageSize,
        Ascending = loadParams.Ascending
      };

      result.Data = await mediator.Send(dataRequest);
    }
    return result;
  }
}
