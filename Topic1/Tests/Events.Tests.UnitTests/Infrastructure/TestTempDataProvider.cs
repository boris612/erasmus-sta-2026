using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Events.Tests.UnitTests.Infrastructure;

internal sealed class TestTempDataProvider : ITempDataProvider
{
    private Dictionary<string, object> values = [];

    public IDictionary<string, object> LoadTempData(HttpContext context)
    {
        return new Dictionary<string, object>(values);
    }

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
        this.values = new Dictionary<string, object>(values);
    }
}
