using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

namespace Events.Tests.UITests.Infrastructure;

internal sealed class UiTestHarness : IAsyncDisposable
{
    private readonly IPlaywright playwright;
    private readonly Process appProcess;
    private readonly StringBuilder processOutput;

    private UiTestHarness(
        IPlaywright playwright,
        IBrowser browser,
        IBrowserContext browserContext,
        IPage page,
        Process appProcess,
        StringBuilder processOutput,
        string rootUrl)
    {
        this.playwright = playwright;
        this.appProcess = appProcess;
        this.processOutput = processOutput;
        Browser = browser;
        BrowserContext = browserContext;
        Page = page;
        RootUrl = rootUrl;
    }

    public IBrowser Browser { get; }

    public IBrowserContext BrowserContext { get; }

    public IPage Page { get; }

    public string RootUrl { get; }

    public static async Task<UiTestHarness> CreateAsync()
    {
        var port = FindFreePort();
        var rootUrl = $"http://127.0.0.1:{port}";
        var mvcProjectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "Events.MVC", "Events.MVC.csproj"));

        var processOutput = new StringBuilder();
        var startInfo = new ProcessStartInfo("dotnet", $"run --project \"{mvcProjectPath}\" --urls {rootUrl}")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "UITest";
        startInfo.Environment["ConnectionStrings__EventDB"] = ResolveUiTestConnectionString();

        var appProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the MVC app process for UI tests.");

        appProcess.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                processOutput.AppendLine(args.Data);
            }
        };
        appProcess.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                processOutput.AppendLine(args.Data);
            }
        };
        appProcess.BeginOutputReadLine();
        appProcess.BeginErrorReadLine();

        await WaitForServerAsync(rootUrl, appProcess, processOutput);

        IPlaywright playwright;
        try
        {
            playwright = await Playwright.CreateAsync();
        }
        catch
        {
            await StopProcessAsync(appProcess);
            throw;
        }

        try
        {
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            var browserContext = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                BaseURL = rootUrl
            });
            var page = await browserContext.NewPageAsync();

            return new UiTestHarness(playwright, browser, browserContext, page, appProcess, processOutput, rootUrl);
        }
        catch (PlaywrightException)
        {
            await StopProcessAsync(appProcess);
            playwright.Dispose();
            throw new InvalidOperationException(
                "Playwright browser is not installed. Run 'dotnet tool install --global Microsoft.Playwright.CLI' and then 'playwright install'.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await BrowserContext.DisposeAsync();
        await Browser.DisposeAsync();
        playwright.Dispose();
        await StopProcessAsync(appProcess);
    }

    private static int FindFreePort()
    {
        using var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        return ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task WaitForServerAsync(string rootUrl, Process appProcess, StringBuilder processOutput)
    {
        using var httpClient = new HttpClient();
        var timeoutAt = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < timeoutAt)
        {
            if (appProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"MVC app process exited before the UI test server became ready.{Environment.NewLine}{processOutput}");
            }

            try
            {
                using var response = await httpClient.GetAsync(rootUrl);
                if ((int)response.StatusCode < 500)
                {
                    return;
                }
            }
            catch
            {
            }

            await Task.Delay(250);
        }

        throw new InvalidOperationException(
            $"Timed out while waiting for the UI test server at {rootUrl}.{Environment.NewLine}{processOutput}");
    }

    private static string ResolveUiTestConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("EventDB-Test");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The EventDB-Test connection string must be available so UI tests can connect to the PostgreSQL test database.");
        }

        return connectionString;
    }

    private static async Task StopProcessAsync(Process appProcess)
    {
        if (appProcess.HasExited)
        {
            appProcess.Dispose();
            return;
        }

        appProcess.Kill(entireProcessTree: true);
        await appProcess.WaitForExitAsync();
        appProcess.Dispose();
    }
}
