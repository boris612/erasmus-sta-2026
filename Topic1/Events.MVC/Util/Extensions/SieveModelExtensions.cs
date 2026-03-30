using Sieve.Models;

namespace Events.MVC.Util.Extensions;

public static class SieveModelExtensions
{
    public static void SetDefaultPagingAndSorting(this SieveModel sieveModel, int defaultPageSize, string defaultSort)
    {
        sieveModel.Page ??= 1;

        if (sieveModel.Page < 1)
        {
            sieveModel.Page = 1;
        }

        if (sieveModel.PageSize is null || sieveModel.PageSize <= 0)
        {
            sieveModel.PageSize = defaultPageSize;
        }

        if (string.IsNullOrWhiteSpace(sieveModel.Sorts))
        {
            sieveModel.Sorts = defaultSort;
        }
    }

    public static string ExtractFilterValue(this SieveModel sieveModel, string propertyName)
    {
        var filters = sieveModel.Filters?.Trim() ?? string.Empty;
        return ExtractFilterValue(filters, propertyName);
    }

    public static string ExtractFilterValue(string filters, string propertyName)
    {
        return ExtractFilterValue(filters, propertyName, "@=*", "@=");
    }

    public static string ExtractFilterValue(string filters, string propertyName, params string[] operators)
    {
        if (string.IsNullOrWhiteSpace(filters))
        {
            return string.Empty;
        }

        foreach (var filter in filters.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var filterOperator in operators)
            {
                var prefix = $"{propertyName}{filterOperator}";
                if (filter.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return filter[prefix.Length..];
                }
            }
        }

        return string.Empty;
    }
}
