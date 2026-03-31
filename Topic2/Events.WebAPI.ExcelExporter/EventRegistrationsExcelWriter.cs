using LargeXlsx;
using Microsoft.EntityFrameworkCore;

namespace Events.WebAPI.ExcelExporter;

internal static class EventRegistrationsExcelWriter
{
  public static async Task WriteAsync(
    string path,
    IQueryable<RowData> rows,
    RowData firstRow,
    CancellationToken cancellationToken)
  {
    await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
    await using var writer = new XlsxWriter(stream);

    string[] headers =
    [
      "Registration ID",
      "Registration date",
      "Person ID",
      "Last name",
      "First name",
      "Last name transcription",
      "First name transcription",
      "Country",
      "Sport"
    ];

    string[] firstRowValues = GetRowValues(firstRow);
    var columns = headers
      .Select((header, index) => XlsxColumn.Formatted(GetWidth(header, firstRowValues[index])))
      .ToArray();

    writer
      .BeginWorksheet("Registrations", columns: columns)
      .BeginRow();

    foreach (string header in headers)
    {
      writer.Write(header);
    }

    await foreach (RowData row in rows.AsAsyncEnumerable().WithCancellation(cancellationToken))
    {
      writer
        .BeginRow()
        .Write(row.RegistrationId)
        .Write(FormatRegisteredAt(row.RegisteredAt))
        .Write(row.PersonId)
        .Write(row.LastName)
        .Write(row.FirstName)
        .Write(row.LastNameTranscription)
        .Write(row.FirstNameTranscription)
        .Write(row.CountryName)
        .Write(row.SportName);
    }

    await writer.CommitAsync();
  }

  private static string[] GetRowValues(RowData row)
  {
    return
    [
      row.RegistrationId.ToString(),
      FormatRegisteredAt(row.RegisteredAt),
      row.PersonId.ToString(),
      row.LastName,
      row.FirstName,
      row.LastNameTranscription,
      row.FirstNameTranscription,
      row.CountryName,
      row.SportName
    ];
  }

  private static double GetWidth(string header, string sample)
  {
    int maxLength = Math.Max(header.Length, sample.Length);
    double paddedWidth = Math.Ceiling(maxLength * 1.25d + 4d);
    return Math.Clamp(paddedWidth, 10d, 60d);
  }

  private static string FormatRegisteredAt(DateTime? value)
  {
    return value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty;
  }

  internal sealed class RowData
  {
    public int RegistrationId { get; init; }
    public DateTime? RegisteredAt { get; init; }
    public int PersonId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FirstNameTranscription { get; init; } = string.Empty;
    public string LastNameTranscription { get; init; } = string.Empty;
    public string CountryName { get; init; } = string.Empty;
    public string SportName { get; init; } = string.Empty;
  }
}
