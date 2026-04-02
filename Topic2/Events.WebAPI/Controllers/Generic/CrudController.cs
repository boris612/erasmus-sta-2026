using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.Queries.Generic;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using MobilityOne.Common.Commands;

namespace Events.WebAPI.Controllers.Generic;


public abstract class CrudController<TDto, TPK> : GetController<TDto, TPK>
   where TDto : class, IHasIdAsPK<TPK>
   where TPK : IEquatable<TPK>
{

  /// <summary>
  /// Creates a new item.    
  /// </summary>
  /// <param name="model">id does not have to be sent (if sent it would be ignored)</param>
  /// <param name="mediator"></param>
  /// <returns>A newly created item</returns>
  /// <response code="201">Returns the newly created item (route to the item, and the item in the body)</response>
  /// <response code="400">If the model is null or not valid</response>  
  [HttpPost]
  [ProducesResponseType(StatusCodes.Status201Created)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [Authorize(Policy = nameof(Policies.EditData))]
  public virtual async Task<ActionResult<TDto>> Create(TDto model, [FromServices] IMediator mediator)
  {
    //Note: It never produce ActionResult<TDto> but we need this because of Swagger description
    //We cannot user generic type in attributes (i.e. in ProducesResponseType)
    //if successful it returns ActionResult with Value:null and Result:CreatedAtAction
    //Thus result.Result.Value is TDto

    var command = new AddCommand<TDto, TPK>(model);
    TPK id = await mediator.Send(command);

    var query = new GetSingleItemQuery<TDto, TPK>(id);

    var item = await mediator.Send(query);   
    var action = CreatedAtAction(nameof(Get), new { id }, item);

    return action;
  }

  /// <summary>
  /// Update the item
  /// </summary>
  /// <param name="id"></param>
  /// <param name="model"></param>
  /// <param name="mediator"></param>
  /// <returns></returns>
  /// <response code="204">if the update was successful</response>
  /// <response code="404">if there is no item with sent id, or if a user does not have a permission to update the item</response>
  /// <response code="400">If the model is not valid</response>
  [HttpPut("{id}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [Authorize(Policy = nameof(Policies.EditData))]
  public virtual async Task<IActionResult> Update(TPK id, TDto model, [FromServices] IMediator mediator)
  {
    if (!model.Id.Equals(id)) //ModelState.IsValid & model != null checked automatically due to [ApiController]
    {
      return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"Different ids: {id} vs {model.Id}");
    }
    else
    {
      var query = new GetSingleItemQuery<TDto, TPK>(id);
      var item = await mediator.Send(query);
      if (item == null)
      {
        return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Invalid id = {id}");
      }

      await DoUpdate(model, mediator);

      return NoContent();
    }
  }

  private static async Task DoUpdate(TDto model, IMediator mediator)
  {
    var command = new UpdateCommand<TDto>(model);
    await mediator.Send(command);   
  }

  /// <summary>
  /// Partially update the item
  /// </summary>
  /// <param name="id"></param>
  /// <param name="delta">RFC 6902 formatted json</param>    
  /// <param name="mediator"></param>
  /// <returns></returns>
  /// <response code="204">if the update was successful</response>
  /// <response code="404">if there is no item with sent id, or if a user does not have a permission to update the item</response>
  /// <response code="400">If the patched model is not valid</response>
  [HttpPatch("{id}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [Authorize(Policy = nameof(Policies.EditData))]
  public virtual async Task<IActionResult> UpdatePartially(TPK id,
                                                           JsonPatchDocument<TDto> delta,
                                                           [FromServices] IMediator mediator)
  {
    //Get current DTO based on id (Get will also check for permission to run patch in contrast to direct retrieval)(
    var getResult = await base.Get(id, mediator);
    if (getResult.Value != null)
    {
      string problem = string.Empty;
      bool ok = true;
      TDto dto = getResult.Value;
      delta.ApplyTo(dto, patchError =>
      {
        ok = false;
        problem = $"{patchError.Operation} causing error: {patchError.ErrorMessage}";
      });
      if (ok)
      {
        if (!dto.Id.Equals(id)) //ensures that id has not been changed
        {
          problem = $"Id mismatch after patching {id} <> {dto.Id}";
          return Problem(detail: problem, statusCode: StatusCodes.Status400BadRequest);
        }
        else
        {
          await DoUpdate(dto, mediator);
          return NoContent();
        }
      }
      else
      {
        return Problem(detail: problem, statusCode: StatusCodes.Status400BadRequest);
      }
    }
    else
    {
      return getResult.Result;
    }
  }

  

  /// <summary>
  /// Delete the item base on primary key value (id)
  /// </summary>
  /// <param name="id">Primary key value</param>
  /// <param name="mediator">Query/Command (Request) mediator. (Obtained using Dependency Injection from services)</param>
  /// <returns></returns>
  /// <response code="204">If the item is deleted</response>
  /// <response code="404">If the item with id does not exist</response>   
  /// <response code="400">If the valiation exists, and fails</response>   
  [HttpDelete("{id}")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [Authorize(Policy = nameof(Policies.EditData))]
  public virtual async Task<IActionResult> Delete(TPK id, [FromServices] IMediator mediator)
  {
    var query = new GetSingleItemQuery<TDto, TPK>(id);
    var item = await mediator.Send(query);
    if (item == null)
    {
      return Problem(statusCode: StatusCodes.Status404NotFound, detail: $"Invalid id = {id}");
    }

    var command = new DeleteCommand<TDto, TPK>(id);
    await mediator.Send(command);   
    return NoContent();
  }
}
