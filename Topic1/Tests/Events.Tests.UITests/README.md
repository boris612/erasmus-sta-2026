# Events.Tests.UITests

This project contains Playwright-based UI tests for `Topic1`.

## Prerequisites

- .NET SDK 8.0
- Playwright CLI
- Playwright browser binaries

## Playwright Installation

Install the Playwright CLI once:

```powershell
dotnet tool install --global Microsoft.Playwright.CLI
```

Install browser binaries:

```powershell
playwright install
```

## Running the UI Tests

Run the full UI test project:

```powershell
dotnet test Topic1\Tests\Events.Tests.UITests\Events.Tests.UITests.csproj
```

Run a single test:

```powershell
dotnet test Topic1\Tests\Events.Tests.UITests\Events.Tests.UITests.csproj --filter HomeAndSportsPageTests.HomePageShouldDisplayEnglishDescription
```

## Notes

- The UI test harness starts the MVC application automatically
- UI tests connect the MVC application to the PostgreSQL test database from `ConnectionStrings:EventDB-Test`
- The browser is currently configured in headless mode
