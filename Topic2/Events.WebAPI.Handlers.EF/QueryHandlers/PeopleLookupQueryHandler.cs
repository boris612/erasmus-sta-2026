using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.LookupQueries;
using Events.WebAPI.Handlers.EF.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Events.WebAPI.Handlers.EF.QueryHandlers;

public class PeopleLookupQueryHandler : IRequestHandler<LookupPeopleQuery, List<IdName<int>>>
{
  private readonly EventsContext ctx;

  public PeopleLookupQueryHandler(EventsContext ctx)
  {
    this.ctx = ctx;
  }

  public async Task<List<IdName<int>>> Handle(LookupPeopleQuery request, CancellationToken cancellationToken)
  {
    var query = ctx.People.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(request.Text))
    {
      string text = request.Text.Trim();
      query = query.Where(p =>
        global::Microsoft.EntityFrameworkCore.EF.Functions.ILike(
          p.FirstNameTranscription + " " + p.LastNameTranscription,
          $"%{text}%"));
    }

    if (!string.IsNullOrWhiteSpace(request.CountryCode))
    {
      string countryCode = request.CountryCode.Trim();
      query = query.Where(p => p.CountryCode == countryCode);
    }

    return await query
      .OrderBy(p => p.FirstNameTranscription)
      .ThenBy(p => p.LastNameTranscription)
      .Select(p => new IdName<int>
      {
        Id = p.Id,
        Name = p.FirstName + " " + p.LastName,
        Description = p.FirstNameTranscription + " " + p.LastNameTranscription
      })
      .ToListAsync(cancellationToken);
  }
}
