using System.Text.Json;
using Events.EFModel.Models;
using Events.MVC.Models;
using Events.MVC.Models.Events;
using Events.MVC.Util.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;

namespace Events.MVC.Controllers;

public class EventsController : Controller
{
    private readonly EventsContext ctx;
    private readonly ISieveProcessor sieveProcessor;
    private readonly PagingSettings pagingSettings;

    public EventsController(EventsContext ctx, ISieveProcessor sieveProcessor, IOptions<PagingSettings> pagingSettings)
    {
        this.ctx = ctx;
        this.sieveProcessor = sieveProcessor;
        this.pagingSettings = pagingSettings.Value;
    }

    public async Task<IActionResult> Index(SieveModel sieveModel)
    {
        var viewModel = await BuildEventsListAsync(sieveModel);
        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return PartialView("_EventsList", viewModel);
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Row(int id)
    {
        var eventModel = await ctx.Events
            .AsNoTracking()
            .Select(e => new EventViewModel
            {
                Id = e.Id,
                Name = e.Name,
                EventDate = e.EventDate,
                ParticipantsCount = e.Registrations.Count
            })
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventModel is null)
        {
            return NotFound();
        }

        return PartialView("_EventRow", eventModel);
    }

    [HttpGet]
    public async Task<IActionResult> EditRow(int id)
    {
        var eventModel = await ctx.Events
            .AsNoTracking()
            .Select(e => new EventViewModel
            {
                Id = e.Id,
                Name = e.Name,
                EventDate = e.EventDate
            })
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventModel is null)
        {
            return NotFound();
        }

        return PartialView("_EventEditRow", eventModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventViewModel model, SieveModel sieveModel)
    {
        if (!ModelState.IsValid)
        {
            Response.Headers["HX-Retarget"] = "#create-event-form";
            Response.Headers["HX-Reswap"] = "outerHTML";
            return PartialView("_CreateEventForm", model);
        }

        var eventEntity = new Event
        {
            Name = model.Name,
            EventDate = model.EventDate
        };
        ctx.Events.Add(eventEntity);
        await ctx.SaveChangesAsync();

        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["event-created"] = true,
            ["show-toast"] = new
            {
                variant = "success",
                title = "Success",
                message = $"Event '{model.Name}' was added successfully."
            }
        });

        var viewModel = await BuildEventsListAsync(sieveModel);
        return PartialView("_EventsList", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return PartialView("_EventEditRow", model);
        }

        var existingEvent = await ctx.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (existingEvent is null)
        {
            return NotFound();
        }

        existingEvent.Name = model.Name;
        existingEvent.EventDate = model.EventDate;
        await ctx.SaveChangesAsync();

        return PartialView("_EventRow", new EventViewModel
        {
            Id = existingEvent.Id,
            Name = existingEvent.Name,
            EventDate = existingEvent.EventDate,
            ParticipantsCount = await ctx.Registrations.CountAsync(r => r.EventId == existingEvent.Id)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, SieveModel sieveModel)
    {
        var eventEntity = await ctx.Events
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (eventEntity is null)
        {
            return NotFound();
        }

        if (eventEntity.Registrations.Count > 0)
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            return Content("The event cannot be deleted because registrations exist.");
        }

        ctx.Events.Remove(eventEntity);
        var deletedName = eventEntity.Name;
        await ctx.SaveChangesAsync();

        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["show-toast"] = new
            {
                variant = "success",
                title = "Success",
                message = $"Event '{deletedName}' was deleted successfully."
            }
        });

        var viewModel = await BuildEventsListAsync(sieveModel);
        return PartialView("_EventsList", viewModel);
    }

    private async Task<PagedList<EventViewModel>> BuildEventsListAsync(SieveModel sieveModel)
    {
        sieveModel.SetDefaultPagingAndSorting(pagingSettings.PageSize, "EventDate");
        var normalizedFilters = sieveModel.Filters?.Trim() ?? string.Empty;
        var nameFilter = SieveModelExtensions.ExtractFilterValue(normalizedFilters, "Name");

        var baseQuery = ctx.Events
            .AsNoTracking()
            .Select(e => new EventViewModel
            {
                Id = e.Id,
                Name = e.Name,
                EventDate = e.EventDate,
                ParticipantsCount = e.Registrations.Count
            });

        var totalCount = await baseQuery.CountAsync();
        var filteredQuery = sieveProcessor.Apply(
            sieveModel,
            baseQuery,
            applyFiltering: true,
            applySorting: false,
            applyPagination: false);
        var filteredCount = await filteredQuery.CountAsync();

        var pagingInfo = new PagingInfo
        {
            FilteredItemsCount = filteredCount,
            TotalItemsCount = totalCount,
            ItemsPerPage = sieveModel.PageSize!.Value,
            CurrentPage = sieveModel.Page!.Value,
            Sorts = sieveModel.Sorts ?? "EventDate",
            Filters = normalizedFilters,
            NameFilter = nameFilter
        };

        if (pagingInfo.CurrentPage > pagingInfo.TotalPages)
        {
            pagingInfo = new PagingInfo
            {
                FilteredItemsCount = filteredCount,
                TotalItemsCount = totalCount,
                ItemsPerPage = pagingInfo.ItemsPerPage,
                CurrentPage = pagingInfo.TotalPages,
                Sorts = pagingInfo.Sorts,
                Filters = pagingInfo.Filters,
                NameFilter = pagingInfo.NameFilter
            };
            sieveModel.Page = pagingInfo.CurrentPage;
        }

        var sortedQuery = sieveProcessor.Apply(sieveModel, baseQuery, applyFiltering: true, applySorting: true, applyPagination: false);
        return await PagedList<EventViewModel>.CreateAsync(sortedQuery, new PagingInfo
        {
            FilteredItemsCount = filteredCount,
            TotalItemsCount = totalCount,
            ItemsPerPage = pagingInfo.ItemsPerPage,
            CurrentPage = pagingInfo.CurrentPage,
            Sorts = pagingInfo.Sorts,
            Filters = pagingInfo.Filters,
            NameFilter = pagingInfo.NameFilter
        });
    }
}
