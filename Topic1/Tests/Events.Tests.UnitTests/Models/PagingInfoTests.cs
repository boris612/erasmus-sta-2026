using Events.MVC.Models;

namespace Events.Tests.UnitTests.Models;

public class PagingInfoTests
{
    public static IEnumerable<object[]> TotalPagesCases =>
    [
        [5, 10, 1],
        [20, 10, 2],
        [25, 10, 3]
    ];

    [Fact]
    public void TotalPages_ReturnsAtLeastOne()
    {
        var pagingInfo = new PagingInfo
        {
            FilteredItemsCount = 0,
            ItemsPerPage = 10,
            CurrentPage = 1
        };

        Assert.Equal(1, pagingInfo.TotalPages);
    }

    [Fact]
    public void TotalPages_RoundsUpWhenFilteredItemsDoNotDivideEvenly()
    {
        var pagingInfo = new PagingInfo
        {
            FilteredItemsCount = 21,
            ItemsPerPage = 10,
            CurrentPage = 1
        };

        Assert.Equal(3, pagingInfo.TotalPages);
    }

    [Theory]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(21, 10, 3)]
    public void TotalPages_ReturnsExpectedPageCount(int filteredItemsCount, int itemsPerPage, int expectedTotalPages)
    {
        var pagingInfo = new PagingInfo
        {
            FilteredItemsCount = filteredItemsCount,
            ItemsPerPage = itemsPerPage,
            CurrentPage = 1
        };

        Assert.Equal(expectedTotalPages, pagingInfo.TotalPages);
    }

    [Theory]
    [MemberData(nameof(TotalPagesCases))]
    public void TotalPages_ReturnsExpectedPageCount_WhenUsingMemberData(
        int filteredItemsCount,
        int itemsPerPage,
        int expectedTotalPages)
    {
        var pagingInfo = new PagingInfo
        {
            FilteredItemsCount = filteredItemsCount,
            ItemsPerPage = itemsPerPage,
            CurrentPage = 1
        };

        Assert.Equal(expectedTotalPages, pagingInfo.TotalPages);
    }

    [Fact]
    public void ToggleSort_ReturnsDescending_WhenAlreadySortedBySameProperty()
    {
        var pagingInfo = new PagingInfo
        {
            Sorts = "Name",
            ItemsPerPage = 10,
            CurrentPage = 1
        };

        Assert.Equal("-Name", pagingInfo.ToggleSort("Name"));
    }

    [Fact]
    public void IsSortedBy_And_IsDescending_ReflectCurrentState()
    {
        var pagingInfo = new PagingInfo
        {
            Sorts = "-RegisteredAt",
            ItemsPerPage = 10,
            CurrentPage = 1
        };

        Assert.True(pagingInfo.IsSortedBy("RegisteredAt"), "PagingInfo should report that sorting is applied by RegisteredAt.");
        Assert.True(pagingInfo.IsDescending(), "PagingInfo should report descending sort order when the sort expression starts with '-'.");
    }
}
