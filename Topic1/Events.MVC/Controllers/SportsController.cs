using System.Text.Json;
using Events.EFModel.Models;
using Events.MVC.Models;
using Events.MVC.Models.Sports;
using Events.MVC.Util.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;

namespace Events.MVC.Controllers;

public class SportsController : Controller
{
    private readonly EventsContext ctx;
    private readonly ISieveProcessor sieveProcessor;
    private readonly PagingSettings pagingSettings;

    public SportsController(EventsContext ctx, ISieveProcessor sieveProcessor, IOptions<PagingSettings> pagingSettings)
    {
        this.ctx = ctx;
        this.sieveProcessor = sieveProcessor;
        this.pagingSettings = pagingSettings.Value;
    }

    public async Task<IActionResult> Index(SieveModel sieveModel)
    {
        var viewModel = await BuildSportsListAsync(sieveModel);
        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return PartialView("_SportsList", viewModel);
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Row(int id)
    {
        var sport = await ctx.Sports
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sport is null)
        {
            return NotFound();
        }

        return PartialView("_SportRow", new SportViewModel
        {
            Id = sport.Id,
            Name = sport.Name
        });
    }

    [HttpGet]
    public async Task<IActionResult> EditRow(int id)
    {
        var sport = await ctx.Sports
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sport is null)
        {
            return NotFound();
        }

        return PartialView("_SportEditRow", new SportViewModel
        {
            Id = sport.Id,
            Name = sport.Name
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SportViewModel model, SieveModel sieveModel)
    {
        if (!ModelState.IsValid)
        {
            Response.Headers["HX-Retarget"] = "#create-sport-form";
            Response.Headers["HX-Reswap"] = "outerHTML";
            return PartialView("_CreateSportForm", model);
        }

        var sport = new Sport
        {
            Name = model.Name
        };
        ctx.Sports.Add(sport);
        await ctx.SaveChangesAsync();

        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["sport-created"] = true,
            ["show-toast"] = new
            {
                variant = "success",
                title = "Success",
                message = $"Sport '{model.Name}' was added successfully."
            }
        });
        var viewModel = await BuildSportsListAsync(sieveModel);
        return PartialView("_SportsList", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SportViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return PartialView("_SportEditRow", model);
        }

        var existingSport = await ctx.Sports.FirstOrDefaultAsync(s => s.Id == id);
        if (existingSport is null)
        {
            return NotFound();
        }

        existingSport.Name = model.Name;
        await ctx.SaveChangesAsync();

        return PartialView("_SportRow", new SportViewModel
        {
            Id = existingSport.Id,
            Name = existingSport.Name
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, SieveModel sieveModel)
    {
        var sport = await ctx.Sports
            .Include(s => s.Registrations)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sport is null)
        {
            return NotFound();
        }

        if (sport.Registrations.Count > 0)
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            return Content("The sport cannot be deleted because registrations exist.");
        }

        ctx.Sports.Remove(sport);
        var deletedName = sport.Name;
        await ctx.SaveChangesAsync();

        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["show-toast"] = new
            {
                variant = "success",
                title = "Success",
                message = $"Sport '{deletedName}' was deleted successfully."
            }
        });
        var viewModel = await BuildSportsListAsync(sieveModel);
        return PartialView("_SportsList", viewModel);
    }

    private async Task<PagedList<SportViewModel>> BuildSportsListAsync(SieveModel sieveModel)
    {
        sieveModel.SetDefaultPagingAndSorting(pagingSettings.PageSize, "Name");
        var normalizedFilters = sieveModel.Filters?.Trim() ?? string.Empty;
        var nameFilter = SieveModelExtensions.ExtractFilterValue(normalizedFilters, "Name");

        var baseQuery = ctx.Sports
            .AsNoTracking()
            .Select(s => new SportViewModel
            {
                Id = s.Id,
                Name = s.Name
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
            Sorts = sieveModel.Sorts ?? "Name",
            Filters = normalizedFilters,
            NameFilter = nameFilter
        };

        if (pagingInfo.CurrentPage > pagingInfo.TotalPages)
        {
            pagingInfo.CurrentPage = pagingInfo.TotalPages;
            sieveModel.Page = pagingInfo.CurrentPage;
        }

        var sortedQuery = sieveProcessor.Apply(sieveModel, baseQuery, applyFiltering: true, applySorting: true, applyPagination: false);
        return await PagedList<SportViewModel>.CreateAsync(sortedQuery, pagingInfo);
    }

}
