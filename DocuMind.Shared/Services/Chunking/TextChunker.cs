namespace DocuMind.Services;

public class TextChunker
{
    private const int ChunkSize = 500;
    private const int Overlap = 80;

    public List<string> Chunk(string text)
    {
        var chunks = new List<string>();

        if (string.IsNullOrWhiteSpace(text))
            return chunks;

        int start = 0;

        while (start < text.Length)
        {
            int end = Math.Min(start + ChunkSize, text.Length);
            bool isLastChunk = (end == text.Length);

            // Try breaking on word boundary — only mid-text, not at the end
            if (!isLastChunk)
            {
                int searchBack = Math.Min(ChunkSize / 2, end - start);
                int lastSpace = text.LastIndexOf(' ', end, searchBack);
                if (lastSpace > start)
                    end = lastSpace;
            }

            var chunk = text[start..end].Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
                chunks.Add(chunk);

            // If we just processed to the end of the text, stop.
            if (isLastChunk) break;

            // Advance with overlap — guarantee we always move forward.
            var next = end - Overlap;
            start = next > start ? next : end;
        }

        return chunks;
    }
}