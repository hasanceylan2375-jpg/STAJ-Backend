using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Pgvector;

namespace STAJ.Services;

public sealed class RagOpenAiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public RagOpenAiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<Vector>> CreateEmbeddingsAsync(IReadOnlyList<string> inputs, CancellationToken cancellationToken)
    {
        var apiKey = GetApiKey();
        var model = _configuration["Rag:EmbeddingModel"] ?? "text-embedding-3-small";
        var baseUrl = (_configuration["Rag:OpenAiBaseUrl"] ?? "https://api.openai.com/v1").TrimEnd('/');

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new { model, input = inputs });

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Embedding servisi isteği başarısız oldu.");

        using var json = JsonDocument.Parse(body);
        var data = json.RootElement.GetProperty("data").EnumerateArray()
            .OrderBy(x => x.GetProperty("index").GetInt32())
            .Select(x => new Vector(x.GetProperty("embedding").EnumerateArray().Select(v => v.GetSingle()).ToArray()))
            .ToList();

        return data;
    }

    public async Task<string> GenerateAnswerAsync(string question, IReadOnlyList<RagRetrievedChunk> context, CancellationToken cancellationToken)
    {
        var apiKey = GetApiKey();
        var model = _configuration["Rag:ChatModel"] ?? "gpt-4.1-mini";
        var baseUrl = (_configuration["Rag:OpenAiBaseUrl"] ?? "https://api.openai.com/v1").TrimEnd('/');

        var contextText = string.Join("\n\n", context.Select((chunk, index) =>
            $"[Kaynak {index + 1}] Dosya: {chunk.FileName}; Şirket: {chunk.CompanyName}; Sayfa: {(chunk.PageNumber?.ToString() ?? "-")}\n{chunk.Text}"));

        var systemPrompt = "Sen şirket içi kuralları cevaplayan bir RAG asistanısın. " +
                           "Yalnızca verilen kaynak metinlerdeki bilgilere dayan. Kaynaklarda cevap yoksa bunu açıkça söyle ve tahmin yürütme. " +
                           "Cevabın sonunda kullandığın kaynakları [Kaynak 1], [Kaynak 2] biçiminde belirt.";

        var payload = new
        {
            model,
            temperature = 0.1,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Soru: {question}\n\nKaynaklar:\n{contextText}" }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("LLM servisi isteği başarısız oldu.");

        using var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
            ?? "Kaynaklardan anlamlı bir cevap üretilemedi.";
    }

    private string GetApiKey()
    {
        var key = _configuration["Rag:OpenAiApiKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Rag:OpenAiApiKey yapılandırılmamış.");
        return key;
    }
}
