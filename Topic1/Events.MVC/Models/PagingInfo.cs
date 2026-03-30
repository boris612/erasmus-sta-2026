namespace Events.MVC.Models;

public class PagingInfo
{
    public int TotalItemsCount { get; init; }

    public int FilteredItemsCount { get; init; }

    public int ItemsPerPage { get; init; }

    public int CurrentPage { get; init; }

    public string Sorts { get; init; } = "Name";

    public string Filters { get; init; } = string.Empty;

    public string NameFilter { get; init; } = string.Empty;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((decimal)FilteredItemsCount / ItemsPerPage));

    public bool IsFiltered => !string.IsNullOrWhiteSpace(Filters);

    public string ToggleSort(string propertyName)
    {
        return string.Equals(Sorts, propertyName, StringComparison.OrdinalIgnoreCase)
            ? $"-{propertyName}"
            : propertyName;
    }

    public bool IsSortedBy(string propertyName)
    {
        return string.Equals(Sorts.TrimStart('-'), propertyName, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsDescending()
    {
        return Sorts.StartsWith("-", StringComparison.Ordinal);
    }
}
