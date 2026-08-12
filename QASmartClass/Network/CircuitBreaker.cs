using System;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace QASmartClass.Network
{
    /// <summary>
    /// Simple circuit‑breaker implementation for async operations.
    /// Guarantees that repeated failures will open the circuit for a cooldown period
    /// before allowing retries. Supports exponential back‑off on each retry.
    /// </summary>
    public class CircuitBreaker
    {
        private readonly int _failureThreshold; // number of consecutive failures to open
        private readonly TimeSpan _openDuration; // how long to stay open
        private readonly TimeSpan _baseDelay; // initial back‑off delay
        private int _failureCount;
        private DateTime _openedAt = DateTime.MinValue;
        private readonly object _lock = new();

        public CircuitBreaker(int failureThreshold = 3, TimeSpan? openDuration = null, TimeSpan? baseDelay = null)
        {
            _failureThreshold = failureThreshold;
            _openDuration = openDuration ?? TimeSpan.FromSeconds(30);
            _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
        }

        private bool IsOpen
        {
            get
            {
                lock (_lock)
                {
                    if (_failureCount >= _failureThreshold)
                    {
                        if (DateTime.UtcNow - _openedAt < _openDuration)
                            return true;
                        // cooldown elapsed – reset
                        _failureCount = 0;
                        _openedAt = DateTime.MinValue;
                    }
                    return false;
                }
            }
        }

        /// <summary>
        /// Executes the supplied async operation respecting the circuit‑breaker state.
        /// Returns the operation result or throws the original exception if the circuit is open.
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            if (IsOpen)
            {
                Log.Warning("Circuit breaker is open – rejecting operation");
                throw new InvalidOperationException("Circuit breaker open");
            }

            int attempt = 0;
            while (true)
            {
                try
                {
                    var result = await operation();
                    // success – reset counters
                    lock (_lock)
                    {
                        _failureCount = 0;
                        _openedAt = DateTime.MinValue;
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    attempt++;
                    lock (_lock)
                    {
                        _failureCount++;
                        if (_failureCount >= _failureThreshold)
                        {
                            _openedAt = DateTime.UtcNow;
                            Log.Warning(ex, "Circuit breaker opened after {Count} failures", _failureCount);
                        }
                    }
                    // if circuit opened, rethrow immediately
                    if (IsOpen)
                        throw new InvalidOperationException("Circuit breaker opened", ex);

                    // exponential back‑off before next retry
                    var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                    Log.Information("Retrying operation after {Delay}s (attempt {Attempt})", delay.TotalSeconds, attempt);
                    await Task.Delay(delay);
                }
            }
        }
    }
}
