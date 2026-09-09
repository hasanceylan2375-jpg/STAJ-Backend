using Microsoft.Extensions.Caching.Memory;

namespace STAJ.Services
{
    public sealed class QuotaService
    {
        private readonly IMemoryCache _cache;
        private readonly object _sync = new();

        public QuotaService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool TryConsume(string key, int dailyLimit, int monthlyLimit, out int dailyRemaining, out int monthlyRemaining)
        {
            lock (_sync)
            {
                var now = DateTime.UtcNow;
                var dailyKey = $"quota:day:{now:yyyy-MM-dd}:{key}";
                var monthlyKey = $"quota:month:{now:yyyy-MM}:{key}";

                var daily = _cache.Get<int>(dailyKey);
                var monthly = _cache.Get<int>(monthlyKey);

                if (daily >= dailyLimit || monthly >= monthlyLimit)
                {
                    dailyRemaining = Math.Max(0, dailyLimit - daily);
                    monthlyRemaining = Math.Max(0, monthlyLimit - monthly);
                    return false;
                }

                daily++;
                monthly++;

                _cache.Set(dailyKey, daily, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = now.Date.AddDays(1)
                });
                _cache.Set(monthlyKey, monthly, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1)
                });

                dailyRemaining = Math.Max(0, dailyLimit - daily);
                monthlyRemaining = Math.Max(0, monthlyLimit - monthly);
                return true;
            }
        }
    }
}
