namespace Events.MVC.Models;

public class PagingInfo
{
    public int TotalItemsCount { get; set; }

    public int FilteredItemsCount { get; set; }

    public int ItemsPerPage { get; set; }

    public int CurrentPage { get; set; }

    public string Sorts { get; set; } = "Name";

    public string Filters { get; set; } = string.Empty;

    public string NameFilter { get; set; } = string.Empty;

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
