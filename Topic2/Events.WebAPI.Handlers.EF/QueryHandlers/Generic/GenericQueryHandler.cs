using AutoMapper;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.Queries.Generic;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sieve.Models;
using Sieve.Services;

namespace Events.WebAPI.Handlers.EF.QueryHandlers.Generic
{
  public abstract class GenericQueryHandler<TDto, TDal, TPK> :
                            IRequestHandler<GetSingleItemQuery<TDto, TPK>, TDto?>,
                            IRequestHandler<GetCountQuery<TDto>, int>,
                            IRequestHandler<DoesItemExistsQuery<TDto, TPK>, bool>,
                            IRequestHandler<GetItemsQuery<TDto>, List<TDto>>
     where TDal : class, IHasIdAsPK<TPK>
     where TPK : IEquatable<TPK>
  {
    private readonly DbContext ctx;
    protected readonly ILogger logger;
    private readonly IMapper mapper;
    private readonly ISieveProcessor sieveProcessor;

    public GenericQueryHandler(DbContext ctx, ILogger logger, IMapper mapper, ISieveProcessor sieveProcessor)
    {
      this.ctx = ctx;
      this.logger = logger;
      this.mapper = mapper;
      this.sieveProcessor = sieveProcessor;
    }

    public virtual async Task<int> Handle(GetCountQuery<TDto> request, CancellationToken cancellationToken)
    {
      var query = ctx.Set<TDal>().AsNoTracking();
      IQueryable<TDto> projectedQuery = mapper.ProjectTo<TDto>(query);
      SieveModel sieveModel = new SieveModel()
      {
        Filters = request.Filters,
      };
      var filteredQuery = sieveProcessor.Apply(sieveModel, projectedQuery, applyFiltering: true, applySorting: false, applyPagination: false);

      int count = await filteredQuery.CountAsync(cancellationToken);
      return count;
    }

    public virtual async Task<TDto?> Handle(GetSingleItemQuery<TDto, TPK> request, CancellationToken cancellationToken)
    {
      var query = ctx.Set<TDal>()
                     .AsNoTracking()
                     .Where(t => t.Id.Equals(request.Id));

      IQueryable<TDto> projectedQuery = mapper.ProjectTo<TDto>(query);
      var item = await projectedQuery.FirstOrDefaultAsync(cancellationToken);
      return item;
    }

    public virtual async Task<bool> Handle(DoesItemExistsQuery<TDto, TPK> request, CancellationToken cancellationToken)
    {
      var query = ctx.Set<TDal>()
                     .AsNoTracking()
                     .Where(t => t.Id.Equals(request.Id));

      bool exists = await query.AnyAsync(cancellationToken);
      return exists;
    }

    public virtual async Task<List<TDto>> Handle(GetItemsQuery<TDto> request, CancellationToken cancellationToken)
    {
      var query = ctx.Set<TDal>().AsNoTracking();
      IQueryable<TDto> projectedQuery = mapper.ProjectTo<TDto>(query);

      SieveModel sieveModel = new SieveModel()
      {
        Filters = request.Filters,
        Sorts = BuildSortExpression(request),
        PageSize = request.PageSize,
        Page = request.Page
      };
      var filteredQuery = sieveProcessor.Apply(sieveModel, projectedQuery, applyFiltering: true, applySorting: true, applyPagination: true);

      var data = await filteredQuery.ToListAsync(cancellationToken);
      return data;
    }

    private static string? BuildSortExpression(GetItemsQuery<TDto> request)
    {
      if (!string.IsNullOrWhiteSpace(request.Sort))
      {
        return request.Ascending ? request.Sort : "-" + request.Sort;
      }

      bool paginationRequested = request.Page.HasValue || request.PageSize.HasValue;
      return paginationRequested ? nameof(IHasIdAsPK<int>.Id) : null;
    }
  }
}
