# Test Plan

## Events.Tests.UnitTests

- `PagingInfo` and `PagedList`
  - `TotalPages` calculation
  - `ToggleSort`, `IsSortedBy`, `IsDescending`
  - pagination behavior and page bounds
- `SieveModelExtensions`
  - normalization of `page`, `pageSize`, and default `sort`
  - extracting filter values for `@=*`, `@=`, and `==`
- controller guards and redirects
  - `People` without countries
  - `Registrations` without events, sports, or people
  - verify that the correct `RedirectToActionResult` is returned and the expected toast is stored in `TempData`
- controller result and view model checks
  - verify whether an action returns the correct `ViewResult`, `PartialViewResult`, `NotFoundResult`, `BadRequestResult`, or `ContentResult`
  - verify that an action returns the expected view model type and expected values inside the view model
- validation and mapping logic from controllers
  - `CountriesController` translations `json <-> view model`
  - duplicate language validation and empty translation row handling
- small helpers and formatting logic
  - toast payload helpers if they are extracted later
  - route/pager helper logic that can be tested without a full MVC host

## Events.Tests.IntegrationTests

- HTTP tests against the MVC application through `WebApplicationFactory`
  - `GET` requests for screens return `200`
  - HTMX requests return the correct partial
- redirect and guard scenarios
  - `People` without countries redirects to `Countries`
  - `Registrations` without events/sports/people redirects to the correct screen
- CRUD happy paths for main screens
  - create/edit/delete for `Sports`, `Events`, `Countries`, and `People`
  - create/edit/delete for `Registrations` for the selected event
- validation and error flow
  - invalid input returns a partial with validation messages
  - DB conflicts return readable `ProblemDetails`
- filtering, sorting, and paging
  - `Sieve` query strings return expected results
  - transcription and country filtering for registrations

For these tests, the best setup is a dedicated PostgreSQL test container or an isolated test database with seeded data.

## Events.Tests.UITests

- end-to-end user flows in the browser
  - open a screen, use the collapse form, save a record, see a toast
  - paging and sorting without duplicating layout
  - HTMX inline edit and cancel
- `Registrations` screen
  - changing the event refreshes the table
  - person autocomplete works and respects the country filter
  - create/edit/delete registration
- navigation smoke tests
  - all main links work
  - redirect messages through toast are displayed

For UI tests, Playwright is the most natural choice, with a small smoke suite and a few critical end-to-end flows.

Suggested local setup:

```powershell
dotnet tool install --global Microsoft.Playwright.CLI
```

Then inside the UI test project:

```powershell
playwright install
```
