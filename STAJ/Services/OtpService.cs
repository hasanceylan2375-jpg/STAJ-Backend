using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace STAJ.Services;

public class OtpService
{
    private sealed record OtpKaydi(string Hash, DateTimeOffset ExpiresAt, DateTimeOffset CreatedAt, int AttemptCount, bool Used, string Kanal, string? KullaniciId, string? IpAddress);

    private readonly ConcurrentDictionary<string, OtpKaydi> _otps = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan OtpLifetime = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(30);
    private const int MaxAttempts = 5;

    public (bool Success, string Message, string? Code) Olustur(string hedef, string kanal, string? kullaniciId, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(hedef)) return (false, "OTP hedefi boş bırakılamaz.", null);
        if (!string.Equals(kanal, "Eposta", StringComparison.OrdinalIgnoreCase))
            return (false, "Bu sürümde OTP gönderim kanalı olarak E-posta desteklenmektedir.", null);

        var key = Normalize(hedef);
        if (_otps.TryGetValue(key, out var mevcut) && DateTimeOffset.UtcNow - mevcut.CreatedAt < ResendCooldown)
            return (false, "Yeni OTP göndermek için lütfen 30 saniye bekleyin.", null);

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var kayit = new OtpKaydi(Hash(code), DateTimeOffset.UtcNow.Add(OtpLifetime), DateTimeOffset.UtcNow, 0, false, kanal, kullaniciId, ipAddress);
        _otps[key] = kayit;
        Temizle();
        return (true, "OTP oluşturuldu.", code);
    }

    public (bool Success, string Message) Dogrula(string hedef, string kod)
    {
        if (string.IsNullOrWhiteSpace(hedef) || string.IsNullOrWhiteSpace(kod))
            return (false, "Hedef ve OTP kodu zorunludur.");

        var key = Normalize(hedef);
        if (!_otps.TryGetValue(key, out var kayit))
            return (false, "Geçerli bir OTP bulunamadı.");
        if (kayit.Used) return (false, "Bu OTP daha önce kullanıldı.");
        if (DateTimeOffset.UtcNow > kayit.ExpiresAt)
        {
            _otps.TryRemove(key, out _);
            return (false, "OTP süresi doldu. Yeni kod isteyin.");
        }
        if (kayit.AttemptCount >= MaxAttempts)
        {
            _otps.TryRemove(key, out _);
            return (false, "Çok fazla hatalı deneme yapıldı. Yeni OTP isteyin.");
        }

        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(kayit.Hash), Convert.FromHexString(Hash(kod))))
        {
            _otps[key] = kayit with { AttemptCount = kayit.AttemptCount + 1 };
            return (false, $"OTP hatalı. Kalan deneme: {Math.Max(0, MaxAttempts - kayit.AttemptCount - 1)}.");
        }

        _otps[key] = kayit with { Used = true };
        return (true, "OTP doğrulama başarılı.");
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private void Temizle()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var item in _otps)
            if (item.Value.ExpiresAt < now) _otps.TryRemove(item.Key, out _);
    }
}
