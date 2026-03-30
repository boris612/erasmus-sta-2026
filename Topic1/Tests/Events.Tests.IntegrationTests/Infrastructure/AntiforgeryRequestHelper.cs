using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace Events.Tests.IntegrationTests.Infrastructure;

internal static partial class AntiforgeryRequestHelper
{
    private const string AntiforgeryFieldName = "__RequestVerificationToken";

    public static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client,
        string pageUrl,
        string postUrl,
        IEnumerable<KeyValuePair<string, string?>> fields)
    {
        var pageHtml = await client.GetStringAsync(pageUrl);
        var token = ExtractAntiforgeryToken(pageHtml);

        var payload = fields
            .Append(new KeyValuePair<string, string?>(AntiforgeryFieldName, token))
            .ToArray();

        using var request = new HttpRequestMessage(HttpMethod.Post, postUrl)
        {
            Content = new FormUrlEncodedContent(payload!)
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        return await client.SendAsync(request);
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "Expected antiforgery token field was not found in the HTML response.");
        return match.Groups["token"].Value;
    }

    [GeneratedRegex("<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AntiforgeryTokenRegex();
}
