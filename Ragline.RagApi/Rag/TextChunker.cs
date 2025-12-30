namespace Ragline.RagApi.Rag;

public static class TextChunker
{
    public static List<string> Chunk(string text, int size = 900, int overlap = 150)
    {
        text = (text ?? "").Replace("\r\n", "\n").Trim();
        var chunks = new List<string>();
        if (text.Length == 0) return chunks;

        int i = 0;
        while (i < text.Length)
        {
            int end = Math.Min(i + size, text.Length);
            var slice = text.Substring(i, end - i).Trim();
            if (slice.Length > 0) chunks.Add(slice);
            if (end == text.Length) break;
            i = Math.Max(0, end - overlap);
        }
        return chunks;
    }
}
