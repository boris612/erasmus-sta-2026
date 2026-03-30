using Events.MVC.Util.Extensions;
using Sieve.Models;

namespace Events.Tests.UnitTests.Util;

public class SieveModelExtensionsTests
{
    [Fact]
    public void SetDefaultPagingAndSorting_AssignsDefaults_WhenValuesAreMissing()
    {
        var model = new SieveModel();

        model.SetDefaultPagingAndSorting(defaultPageSize: 10, defaultSort: "Name");

        Assert.Equal(1, model.Page);
        Assert.Equal(10, model.PageSize);
        Assert.Equal("Name", model.Sorts);
    }

    [Fact]
    public void SetDefaultPagingAndSorting_ClampsInvalidPage_AndPageSize()
    {
        var model = new SieveModel
        {
            Page = 0,
            PageSize = -5
        };

        model.SetDefaultPagingAndSorting(defaultPageSize: 20, defaultSort: "RegisteredAt");

        Assert.Equal(1, model.Page);
        Assert.Equal(20, model.PageSize);
        Assert.Equal("RegisteredAt", model.Sorts);
    }

    [Theory]
    [InlineData("Name@=*basketball", "Name", "basketball")]
    [InlineData("Name@=basketball", "Name", "basketball")]
    [InlineData("PersonTranscription@=*ivan", "PersonTranscription", "ivan")]
    [InlineData(" PersonTranscription@=*ivan ", "PersonTranscription", "ivan")]
    public void ExtractFilterValue_ReturnsExpectedValue_ForDefaultOperators(
        string filters,
        string propertyName,
        string expected)
    {
        var value = SieveModelExtensions.ExtractFilterValue(filters, propertyName);

        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("CountryCode==HR", "CountryCode", "HR")]
    [InlineData("PersonTranscription@=*ivan,CountryCode==HR", "CountryCode", "HR")]
    [InlineData("CountryCode==HR,PersonTranscription@=*ivan", "CountryCode", "HR")]
    public void ExtractFilterValue_ReturnsExpectedValue_ForCustomOperator(
        string filters,
        string propertyName,
        string expected)
    {
        var value = SieveModelExtensions.ExtractFilterValue(filters, propertyName, "==");

        Assert.Equal(expected, value);
    }

    [Fact]
    public void ExtractFilterValue_ReturnsEmptyString_WhenPropertyIsMissing()
    {
        var value = SieveModelExtensions.ExtractFilterValue(
            "PersonTranscription@=*ivan,CountryCode==HR",
            "Name");

        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public void ExtractFilterValue_FromModel_UsesFiltersProperty()
    {
        var model = new SieveModel
        {
            Filters = "FullName@=*ana"
        };

        var value = model.ExtractFilterValue("FullName");

        Assert.Equal("ana", value);
    }
}
