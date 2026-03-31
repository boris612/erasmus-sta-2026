using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.LookupQueries;
using Events.WebAPI.Handlers.EF.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Events.WebAPI.Handlers.EF.QueryHandlers;

public class CountriesLookupQueryHandler : IRequestHandler<LookupCountryQuery, List<IdName<string>>>
{
  private readonly EventsContext ctx;

  public CountriesLookupQueryHandler(EventsContext ctx)
  {
    this.ctx = ctx;
  }

  public async Task<List<IdName<string>>> Handle(LookupCountryQuery request, CancellationToken cancellationToken)
  {
    var query = ctx.Countries.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(request.Text))
    {
      string text = request.Text.Trim();
      query = query.Where(c => Microsoft.EntityFrameworkCore.EF.Functions.ILike(c.Name, $"%{text}%"));
    }

    return await query
      .OrderBy(c => c.Name)
      .Select(c => new IdName<string>
      {
        Id = c.Code,
        Name = c.Name
      })
      .ToListAsync(cancellationToken);
  }
}
