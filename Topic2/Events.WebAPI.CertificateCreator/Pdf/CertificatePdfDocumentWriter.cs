using System.Text;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;
using PdfSharpCore.Utils;

namespace Events.WebAPI.CertificateCreator.Pdf;

internal static class CertificatePdfDocumentWriter
{
  private const string FontFamilyName = "Arial";
  private static int initialized;

  public static byte[] CreateCertificate(CertificatePdfModel model)
  {
    EnsureFontsConfigured();

    using var document = new PdfDocument();
    PdfPage page = document.AddPage();
    page.Size = PageSize.A4;

    using XGraphics graphics = XGraphics.FromPdfPage(page);
    var titleFont = new XFont(FontFamilyName, 20, XFontStyle.Bold);
    var headingFont = new XFont(FontFamilyName, 13, XFontStyle.Bold);
    var textFont = new XFont(FontFamilyName, 12, XFontStyle.Regular);

    double marginLeft = 50;
    double y = 60;
    double contentWidth = page.Width - marginLeft * 2;

    graphics.DrawString(model.Title, titleFont, XBrushes.DarkBlue, new XRect(marginLeft, y, contentWidth, 30), XStringFormats.TopLeft);
    y += 52;

    foreach (string paragraph in BuildParagraphs(model))
    {
      DrawParagraph(graphics, paragraph, textFont, marginLeft, ref y, contentWidth);
      y += 8;
    }

    graphics.DrawString("Sports", headingFont, XBrushes.Black, new XRect(marginLeft, y, contentWidth, 20), XStringFormats.TopLeft);
    y += 26;

    foreach (string sportName in model.SportNames)
    {
      DrawParagraph(graphics, $"- {sportName}", textFont, marginLeft + 12, ref y, contentWidth - 12);
      y += 4;
    }

    y += 12;
    DrawParagraph(graphics, $"Event ID: {model.EventId}", textFont, marginLeft, ref y, contentWidth);
    y += 4;
    DrawParagraph(graphics, $"Person ID: {model.PersonId}", textFont, marginLeft, ref y, contentWidth);

    using var stream = new MemoryStream();
    document.Save(stream, false);
    return stream.ToArray();
  }

  private static void EnsureFontsConfigured()
  {
    if (Interlocked.Exchange(ref initialized, 1) == 1)
      return;

    GlobalFontSettings.FontResolver = new FontResolver();
  }

  private static IEnumerable<string> BuildParagraphs(CertificatePdfModel model)
  {
    yield return $"This confirms that {model.PersonFullName} participated in the event \"{model.EventName}\".";

    if (!string.IsNullOrWhiteSpace(model.PersonFullNameTranscription) &&
        !string.Equals(model.PersonFullName, model.PersonFullNameTranscription, StringComparison.OrdinalIgnoreCase))
    {
      yield return $"Transcribed full name: {model.PersonFullNameTranscription}.";
    }

    yield return $"Event date: {model.EventDate:dd.MM.yyyy}.";
    yield return "The person competed in the following sports:";
  }

  private static void DrawParagraph(XGraphics graphics, string text, XFont font, double left, ref double y, double width)
  {
    foreach (string line in WrapText(graphics, text, font, width))
    {
      graphics.DrawString(line, font, XBrushes.Black, new XRect(left, y, width, 18), XStringFormats.TopLeft);
      y += 18;
    }
  }

  private static IEnumerable<string> WrapText(XGraphics graphics, string text, XFont font, double width)
  {
    if (string.IsNullOrWhiteSpace(text))
    {
      yield return string.Empty;
      yield break;
    }

    var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var lineBuilder = new StringBuilder();

    foreach (string word in words)
    {
      string candidate = lineBuilder.Length == 0 ? word : $"{lineBuilder} {word}";
      if (graphics.MeasureString(candidate, font).Width <= width)
      {
        lineBuilder.Clear();
        lineBuilder.Append(candidate);
        continue;
      }

      if (lineBuilder.Length > 0)
      {
        yield return lineBuilder.ToString();
        lineBuilder.Clear();
        lineBuilder.Append(word);
      }
      else
      {
        yield return word;
      }
    }

    if (lineBuilder.Length > 0)
      yield return lineBuilder.ToString();
  }
}
