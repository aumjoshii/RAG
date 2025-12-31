using System.Net.Http.Json;

namespace Ragline.RagApi.Services;

public class OllamaClient
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OllamaClient(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _model = cfg["Ollama:Model"] ?? "llama3.2:3b";
    }

    public async Task<string> GenerateAsync(string prompt)
    {
        var resp = await _http.PostAsJsonAsync("/api/generate", new
        {
            model = _model,
            prompt,
            stream = false
        });

        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<GenResp>();
        return json?.response?.Trim() ?? "";
    }

    public async Task<bool> IsHealthyAsync()
    {
        var resp = await _http.GetAsync("/api/tags");
        return resp.IsSuccessStatusCode;
    }


    private class GenResp { public string? response { get; set; } }
}
