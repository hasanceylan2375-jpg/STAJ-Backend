using System.Text.Json;
using System.Text.Json.Serialization;

namespace STAJ.Services;

public class CaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public CaptchaService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<bool> VerifyAsync(string? token, string? remoteIp = null)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        var secret = _configuration["Recaptcha:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret)) return false;

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"] = secret,
            ["response"] = token,
            ["remoteip"] = remoteIp ?? string.Empty
        });

        using var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
        if (!response.IsSuccessStatusCode) return false;

        var result = await response.Content.ReadFromJsonAsync<RecaptchaVerifyResponse>();
        return result?.Success == true;
    }

    private sealed class RecaptchaVerifyResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}
