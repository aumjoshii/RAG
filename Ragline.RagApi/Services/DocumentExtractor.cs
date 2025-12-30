using UglyToad.PdfPig;
using Ragline.RagApi.Data;
using Ragline.RagApi.Rag;
using Ragline.RagApi.Services;


namespace Ragline.RagApi.Services;

public record ExtractedPage(int PageNumber, string Text);

public static class DocumentExtractor
{
    public static List<ExtractedPage> Extract(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext == ".txt")
        {
            return new List<ExtractedPage> { new(1, File.ReadAllText(filePath)) };
        }
        if (ext == ".pdf")
        {
            var pages = new List<ExtractedPage>();
            using var doc = PdfDocument.Open(filePath);
            foreach (var p in doc.GetPages())
            {
                pages.Add(new ExtractedPage(p.Number, p.Text ?? ""));
            }
            return pages;
        }
        throw new InvalidOperationException("Only .pdf and .txt are supported");
    }
}
