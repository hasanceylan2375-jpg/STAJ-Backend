namespace STAJ.Services
{
    public sealed class CircuitBreakerService
    {
        private readonly object _sync = new();
        private int _consecutiveFailures;
        private DateTime _openUntilUtc = DateTime.MinValue;

        public bool IsOpen
        {
            get
            {
                lock (_sync)
                    return DateTime.UtcNow < _openUntilUtc;
            }
        }

        public void RecordSuccess()
        {
            lock (_sync)
            {
                _consecutiveFailures = 0;
                _openUntilUtc = DateTime.MinValue;
            }
        }

        public void RecordFailure(int failureThreshold, TimeSpan breakDuration)
        {
            lock (_sync)
            {
                _consecutiveFailures++;
                if (_consecutiveFailures >= failureThreshold)
                {
                    _openUntilUtc = DateTime.UtcNow.Add(breakDuration);
                    _consecutiveFailures = 0;
                }
            }
        }
    }
}
