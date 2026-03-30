using Events.Tests.UITests.Infrastructure;
using Microsoft.Playwright;

namespace Events.Tests.UITests;

public class HomeAndSportsPageTests
{
    [Fact]
    public async Task HomePageShouldDisplayEnglishDescription()
    {
        await using var harness = await UiTestHarness.CreateAsync();

        await harness.Page.GotoAsync($"{harness.RootUrl}/");

        await Assertions.Expect(harness.Page.GetByRole(AriaRole.Heading, new() { Name = "Events" })).ToBeVisibleAsync();
        await Assertions.Expect(harness.Page.GetByText("This sample demonstrates an ASP.NET Core MVC application")).ToBeVisibleAsync();
        await Assertions.Expect(harness.Page.GetByRole(AriaRole.Link, new() { Name = "Sports" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SportsPageShouldDisplayExistingSportsAndAllowOpeningCreatePanel()
    {
        await using var harness = await UiTestHarness.CreateAsync();
        var expectedSports = new[] { "Running", "Chess", "Swimming" };

        await harness.Page.GotoAsync($"{harness.RootUrl}/Sports");

        await Assertions.Expect(harness.Page.GetByRole(AriaRole.Heading, new() { Name = "Sports list" })).ToBeVisibleAsync();
        await Assertions.Expect(harness.Page.Locator("#sports-table-body tr").First).ToBeVisibleAsync();
        await Assertions.Expect(harness.Page.GetByPlaceholder("Search by sport name")).ToBeVisibleAsync();
        foreach (var expectedSport in expectedSports)
        {
            await Assertions.Expect(harness.Page.Locator("#sports-list")).ToContainTextAsync(expectedSport);
        }

        await harness.Page.GetByRole(AriaRole.Button, new() { Name = "New sport" }).ClickAsync();

        await Assertions.Expect(harness.Page.GetByRole(AriaRole.Heading, new() { Name = "Add a new sport" })).ToBeVisibleAsync();
        await Assertions.Expect(
            harness.Page.Locator("#create-sport-form").GetByLabel("Name", new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task SportsPageShouldCreateSportAndShowSuccessToast()
    {
        await using var harness = await UiTestHarness.CreateAsync();
        var sportName = $"UI Test Sport {Guid.NewGuid():N}";

        await harness.Page.GotoAsync($"{harness.RootUrl}/Sports");
        await harness.Page.GetByRole(AriaRole.Button, new() { Name = "New sport" }).ClickAsync();

        var createForm = harness.Page.Locator("#create-sport-form");
        await createForm.GetByLabel("Name", new() { Exact = true }).FillAsync(sportName);
        await createForm.GetByRole(AriaRole.Button, new() { Name = "Add sport" }).ClickAsync();

        await Assertions.Expect(harness.Page.Locator("#app-toast")).ToBeVisibleAsync();
        await Assertions.Expect(harness.Page.Locator("#app-toast-title")).ToHaveTextAsync("Success");
        await Assertions.Expect(harness.Page.Locator("#app-toast-body")).ToContainTextAsync($"Sport '{sportName}' was added successfully.");

        await harness.Page.GetByPlaceholder("Search by sport name").FillAsync(sportName);
        await harness.Page.GetByRole(AriaRole.Button, new() { Name = "Filter" }).ClickAsync();

        await Assertions.Expect(harness.Page.Locator("#sports-list")).ToContainTextAsync(sportName);

        var sportRow = harness.Page.Locator("#sports-table-body tr").Filter(new() { HasText = sportName });
        await Assertions.Expect(sportRow).ToHaveCountAsync(1);

        harness.Page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await sportRow.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        await Assertions.Expect(harness.Page.Locator("#app-toast")).ToBeVisibleAsync();
        await Assertions.Expect(harness.Page.Locator("#app-toast-title")).ToHaveTextAsync("Success");
        await Assertions.Expect(harness.Page.Locator("#app-toast-body")).ToContainTextAsync($"Sport '{sportName}' was deleted successfully.");
        await Assertions.Expect(harness.Page.Locator("#sports-list")).Not.ToContainTextAsync(sportName);
    }
}
