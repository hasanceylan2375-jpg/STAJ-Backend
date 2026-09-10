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
        int answer;
        string question;

        switch (operation)
        {
            case 0:
                answer = first + second;
                question = $"{first} + {second} = ?";
                break;
            case 1:
                answer = first;
                question = $"{first + second} - {second} = ?";
                break;
            default:
                var firstFactor = RandomNumberGenerator.GetInt32(2, 10);
                var secondFactor = RandomNumberGenerator.GetInt32(2, 10);
                answer = firstFactor * secondFactor;
                question = $"{firstFactor} × {secondFactor} = ?";
                break;
        }

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
}

public record CaptchaChallenge(string Id, string Question);
