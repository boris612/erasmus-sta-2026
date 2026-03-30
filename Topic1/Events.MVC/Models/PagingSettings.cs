namespace Events.MVC.Models;

public class PagingSettings
{
    public const string SectionName = "Paging";

    public int PageSize { get; set; } = 2;

    public int PageOffset { get; set; } = 5;
}
