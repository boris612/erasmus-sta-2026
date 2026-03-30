using Events.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;

namespace Events.MVC.Util.TagHelpers;

[HtmlTargetElement("pager", Attributes = "page-info,page-action")]
public class PagerTagHelper : TagHelper
{
    private readonly IUrlHelperFactory urlHelperFactory;
    private readonly PagingSettings pagingSettings;

    public PagerTagHelper(IUrlHelperFactory urlHelperFactory, IOptions<PagingSettings> pagingSettings)
    {
        this.urlHelperFactory = urlHelperFactory;
        this.pagingSettings = pagingSettings.Value;
    }

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public PagingInfo PageInfo { get; set; } = new();

    public string PageAction { get; set; } = string.Empty;

    public string PageTitle { get; set; } = "Unesite broj stranice";

    public string? PageTarget { get; set; }

    public string? PageSwap { get; set; }

    public bool PagePushUrl { get; set; }

    [HtmlAttributeName(DictionaryAttributePrefix = "page-route-")]
    public Dictionary<string, string> PageRouteValues { get; set; } = [];

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "nav";
        output.Attributes.SetAttribute("aria-label", "Pager");

        var paginationList = new TagBuilder("ul");
        paginationList.AddCssClass("pagination");
        paginationList.AddCssClass("mb-0");

        var firstPageInRange = Math.Max(1, PageInfo.CurrentPage - pagingSettings.PageOffset);
        var lastPageInRange = Math.Min(PageInfo.TotalPages, PageInfo.CurrentPage + pagingSettings.PageOffset);

        if (firstPageInRange > 1)
        {
            paginationList.InnerHtml.AppendHtml(BuildListItemForPage(1, "1.."));
        }

        for (var page = firstPageInRange; page <= lastPageInRange; page++)
        {
            paginationList.InnerHtml.AppendHtml(
                page == PageInfo.CurrentPage
                    ? BuildListItemForCurrentPage(page)
                    : BuildListItemForPage(page));
        }

        if (lastPageInRange < PageInfo.TotalPages)
        {
            paginationList.InnerHtml.AppendHtml(BuildListItemForPage(PageInfo.TotalPages, $"..{PageInfo.TotalPages}"));
        }

        output.Content.AppendHtml(paginationList);
    }

    private TagBuilder BuildListItemForPage(int page)
    {
        return BuildListItemForPage(page, page.ToString());
    }

    private TagBuilder BuildListItemForPage(int page, string text)
    {
        var urlHelper = urlHelperFactory.GetUrlHelper(ViewContext);
        var url = urlHelper.Action(PageAction, BuildRouteValues(page)) ?? string.Empty;

        var anchor = new TagBuilder("a");
        anchor.InnerHtml.Append(text);
        anchor.Attributes["href"] = url;
        anchor.AddCssClass("page-link");
        ApplyHtmxAttributes(anchor, url);

        var listItem = new TagBuilder("li");
        listItem.AddCssClass("page-item");
        listItem.InnerHtml.AppendHtml(anchor);
        return listItem;
    }

    private TagBuilder BuildListItemForCurrentPage(int page)
    {
        var urlHelper = urlHelperFactory.GetUrlHelper(ViewContext);
        var urlTemplate = urlHelper.Action(PageAction, BuildRouteValues("__page__")) ?? string.Empty;

        var input = new TagBuilder("input");
        input.Attributes["type"] = "text";
        input.Attributes["value"] = page.ToString();
        input.Attributes["data-current"] = page.ToString();
        input.Attributes["data-min"] = "1";
        input.Attributes["data-max"] = PageInfo.TotalPages.ToString();
        input.Attributes["data-url-template"] = urlTemplate;
        input.Attributes["title"] = PageTitle;
        if (!string.IsNullOrWhiteSpace(PageTarget))
        {
            input.Attributes["data-target"] = PageTarget;
        }

        if (!string.IsNullOrWhiteSpace(PageSwap))
        {
            input.Attributes["data-swap"] = PageSwap;
        }

        input.Attributes["data-push-url"] = PagePushUrl.ToString().ToLowerInvariant();
        input.AddCssClass("page-link");
        input.AddCssClass("pagebox");

        var listItem = new TagBuilder("li");
        listItem.AddCssClass("page-item");
        listItem.AddCssClass("active");
        listItem.InnerHtml.AppendHtml(input);

        return listItem;
    }

    private void ApplyHtmxAttributes(TagBuilder tagBuilder, string url)
    {
        if (string.IsNullOrWhiteSpace(PageTarget))
        {
            return;
        }

        tagBuilder.Attributes["hx-get"] = url;
        tagBuilder.Attributes["hx-target"] = PageTarget;
        tagBuilder.Attributes["hx-swap"] = string.IsNullOrWhiteSpace(PageSwap) ? "outerHTML" : PageSwap;
        if (PagePushUrl)
        {
            tagBuilder.Attributes["hx-push-url"] = "true";
        }
    }

    private RouteValueDictionary BuildRouteValues(object pageValue)
    {
        var routeValues = new RouteValueDictionary(PageRouteValues.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value));
        routeValues["page"] = pageValue;
        routeValues["pageSize"] = PageInfo.ItemsPerPage;
        routeValues["sorts"] = PageInfo.Sorts;
        routeValues["filters"] = PageInfo.Filters;
        return routeValues;
    }
}
