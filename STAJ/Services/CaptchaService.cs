using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace STAJ.Services;

public class CaptchaService
{
    private readonly IMemoryCache _cache;
    private const string CachePrefix = "captcha:";

    public CaptchaService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public CaptchaChallenge Create()
    {
        var first = RandomNumberGenerator.GetInt32(1, 20);
        var second = RandomNumberGenerator.GetInt32(1, 20);
        var operation = RandomNumberGenerator.GetInt32(0, 3);

        var question = operation switch
        {
            0 => $"{first} + {second} = ?",
            1 => $"{first + second} - {second} = ?",
            _ => $"{RandomNumberGenerator.GetInt32(2, 10)} × {RandomNumberGenerator.GetInt32(2, 10)} = ?"
        };

        var answer = operation switch
        {
            0 => first + second,
            1 => first,
            _ => ExtractMultiplicationAnswer(question)
        };

        var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        _cache.Set(CachePrefix + id, answer, TimeSpan.FromMinutes(2));
        return new CaptchaChallenge(id, question);
    }

    public bool Validate(string? id, string? answer)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(answer)) return false;
        if (!_cache.TryGetValue(CachePrefix + id, out int expected)) return false;

        _cache.Remove(CachePrefix + id);
        return int.TryParse(answer.Trim(), out var actual) && actual == expected;
    }

    private static int ExtractMultiplicationAnswer(string question)
    {
        var parts = question.Replace(" = ?", string.Empty).Split('×', StringSplitOptions.TrimEntries);
        return int.Parse(parts[0]) * int.Parse(parts[1]);
    }
}

public record CaptchaChallenge(string Id, string Question);
