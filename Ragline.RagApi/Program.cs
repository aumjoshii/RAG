using Ragline.RagApi.Services;
using Ragline.RagApi.Data;
using Ragline.RagApi.Rag;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

Db.Init();
builder.Services.AddSingleton<RagRepository>();


builder.Services.AddHttpClient<EmbeddingsClient>(c =>
{
    c.BaseAddress = new Uri("http://127.0.0.1:8001");
    c.Timeout = TimeSpan.FromMinutes(2);
});

builder.Services.AddHttpClient<OllamaClient>(c =>
{
    c.BaseAddress = new Uri("http://127.0.0.1:11434");
    c.Timeout = TimeSpan.FromMinutes(5);
});


builder.Services.AddOpenApi();
builder.Services.AddControllers();


var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();



app.MapGet("/health", () => Results.Ok(new { ok = true }));

app.MapGet("/stats", (RagRepository repo) =>
{
    return Results.Ok(new { totalChunks = repo.CountChunks() });
});



app.MapGet("/embed-test", async (EmbeddingsClient emb) =>
{
    var vecs = await emb.EmbedAsync(new List<string> { "hello from dotnet" });
    return Results.Ok(new { length = vecs[0].Length, first5 = vecs[0].Take(5).ToArray() });
});

app.MapPost("/ask", async (HttpRequest request, RagRepository repo, EmbeddingsClient emb, OllamaClient llm) =>
{
    var body = await request.ReadFromJsonAsync<AskReq>();
    if (body == null || string.IsNullOrWhiteSpace(body.question))
        return Results.BadRequest("Expected { question: string, topK?: number }");

    var topK = body.topK is null ? 5 : Math.Clamp(body.topK.Value, 1, 10);

    var qVec = (await emb.EmbedAsync(new List<string> { body.question! }))[0];

    var all = repo.GetAllChunks();
    var scored = all
        .Select(c => new
        {
            c.fileName,
            c.page,
            c.chunkIndex,
            c.text,
            score = VectorMath.Cosine(qVec, c.vec)
        })
        .OrderByDescending(x => x.score)
        .Take(topK)
        .ToList();

    var context = string.Join("\n\n", scored.Select((s, i) =>
        $"[Source {i + 1}] file={s.fileName}, page={(s.page?.ToString() ?? "n/a")}, chunk={s.chunkIndex}\n{s.text}"
    ));

    var prompt =
$@"You are a helpful assistant. Answer using ONLY the provided sources.
If the sources do not contain the answer, say: ""I don't know based on the uploaded documents.""

Question: {body.question}

Sources:
{context}

Answer:";

    var answer = await llm.GenerateAsync(prompt);

    var sources = scored.Select(s => new
    {
        s.fileName,
        s.page,
        s.chunkIndex,
        s.score,
        text = s.text.Length > 300 ? s.text[..300] + "…" : s.text
    });

    return Results.Ok(new { answer, sources });
});

app.MapGet("/documents", (RagRepository repo) =>
{
    var docs = repo.ListDocuments().Select(d => new
    {
        id = d.Id,
        fileName = d.FileName,
        createdUtc = d.CreatedUtc
    });

    return Results.Ok(docs);
});



app.MapPost("/documents", async (HttpRequest request, RagRepository repo, EmbeddingsClient emb) =>
{
    Console.WriteLine(">> /documents hit");

    if (!request.HasFormContentType) return Results.BadRequest("Expected multipart/form-data");
    var form = await request.ReadFormAsync();
    Console.WriteLine($">> files count = {form.Files.Count}");

    var file = form.Files.FirstOrDefault();
    if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded");

    Console.WriteLine($">> file received: {file.FileName}, size={file.Length}");


    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (ext is not (".pdf" or ".txt")) return Results.BadRequest("Only .pdf or .txt");

    var uploadsDir = Path.Combine(AppContext.BaseDirectory, "uploads");
    Directory.CreateDirectory(uploadsDir);

    var savedPath = Path.Combine(uploadsDir, $"{Guid.NewGuid():N}{ext}");
    using (var fs = File.Create(savedPath))
        await file.CopyToAsync(fs);

    var docId = repo.InsertDocument(file.FileName);

    var pages = DocumentExtractor.Extract(savedPath);

    var allChunks = new List<(int chunkIndex, int? page, string text)>();
    int idx = 0;
    foreach (var p in pages)
    {
        foreach (var ch in TextChunker.Chunk(p.Text))
            allChunks.Add((idx++, p.PageNumber, ch));
    }

    // Embed in batches (keeps memory sane)
    const int batch = 32;
    var toInsert = new List<(int chunkIndex, int? page, string text, float[] vec)>();

    for (int i = 0; i < allChunks.Count; i += batch)
    {
        var slice = allChunks.Skip(i).Take(batch).ToList();
        var vecs = await emb.EmbedAsync(slice.Select(s => s.text).ToList());
        for (int j = 0; j < slice.Count; j++)
            toInsert.Add((slice[j].chunkIndex, slice[j].page, slice[j].text, vecs[j]));
    }

    repo.InsertChunks(docId, toInsert);

    return Results.Ok(new { documentId = docId, chunksIndexed = allChunks.Count, totalChunksInDb = repo.CountChunks() });
});






// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
