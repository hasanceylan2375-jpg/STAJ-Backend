using System.Collections.Concurrent;

namespace STAJ.Services
{
    public sealed class QuotaService
    {
        private sealed class Counter
        {
            public int Daily;
            public int Monthly;
        }

        private readonly ConcurrentDictionary<string, Counter> _counters = new();

        public bool TryConsume(string key, int dailyLimit, int monthlyLimit, out int dailyRemaining, out int monthlyRemaining)
        {
            var counter = _counters.GetOrAdd(BuildKey(key), _ => new Counter());

            lock (counter)
            {
                if (counter.Daily >= dailyLimit || counter.Monthly >= monthlyLimit)
                {
                    dailyRemaining = Math.Max(0, dailyLimit - counter.Daily);
                    monthlyRemaining = Math.Max(0, monthlyLimit - counter.Monthly);
                    return false;
                }

                counter.Daily++;
                counter.Monthly++;
                dailyRemaining = Math.Max(0, dailyLimit - counter.Daily);
                monthlyRemaining = Math.Max(0, monthlyLimit - counter.Monthly);
                return true;
            }
        }

        private static string BuildKey(string key)
        {
            var now = DateTime.UtcNow;
            return $"{key}:{now:yyyy-MM}:{now:yyyy-MM-dd}";
        }
    }
}
