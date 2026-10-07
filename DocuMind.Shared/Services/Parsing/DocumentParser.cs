using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

namespace DocuMind.Services;

public sealed class DocumentParser
{
    private readonly ILogger<DocumentParser> _logger;

    public DocumentParser(ILogger<DocumentParser> logger)
    {
        _logger = logger;
        _logger.LogInformation("[PARSER] DocumentParser ready");
    }

    public Task<string> ParseAsync(
        string fileName,
        Stream fileStream,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        _logger.LogInformation(
            "[PARSER] ParseAsync file={FileName}, ext={Ext}",
            fileName,
            ext);

        var text = ext switch
        {
            ".pdf" => ExtractPdf(fileStream),
            ".docx" => ExtractDocx(fileStream),
            ".txt" => ExtractText(fileStream),
            _ => ExtractText(fileStream)
        };

        _logger.LogInformation(
            "[PARSER] Parse done ext={Ext}, chars={Chars}",
            ext,
            text.Length);

        return Task.FromResult(text);
    }

    private string ExtractPdf(Stream stream)
    {
        using var pdf = PdfDocument.Open(stream);
        var sb = new StringBuilder();

        foreach (var page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }

    private string ExtractDocx(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, isEditable: false);
        var body = doc.MainDocumentPart?.Document?.Body;

        if (body is null)
            return string.Empty;

        var sb = new StringBuilder();

        foreach (var para in body.Descendants<Paragraph>())
        {
            var line = para.InnerText?.Trim();

            if (!string.IsNullOrWhiteSpace(line))
                sb.AppendLine(line);
        }

        return sb.ToString();
    }

    private string ExtractText(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}