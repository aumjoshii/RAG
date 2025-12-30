using System.Net.Http.Json;

namespace Ragline.RagApi.Services;

public class EmbeddingsClient
{
    private readonly HttpClient _http;

    public EmbeddingsClient(HttpClient http) => _http = http;

    public async Task<List<float[]>> EmbedAsync(List<string> texts)
    {
        var resp = await _http.PostAsJsonAsync("/embed", new { texts });
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<EmbedResp>();
        if (json?.vectors == null) throw new Exception("Embedding server returned null vectors");

        return json.vectors
            .Select(v => v.Select(x => (float)x).ToArray())
            .ToList();
    }

    private class EmbedResp
    {
        public List<List<double>>? vectors { get; set; }
    }
}
