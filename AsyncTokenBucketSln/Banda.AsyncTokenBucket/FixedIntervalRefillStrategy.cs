using Nito.AsyncEx;
using System;
using System.Threading.Tasks;

namespace Banda.AsyncTokenBucket
{
    public class FixedIntervalRefillStrategy : IRefillStrategy
    {
        private readonly Ticker _ticker;
        private readonly long _numTokens;
        private readonly long _periodInTicks;
        private long _nextRefillTime;
        private readonly AsyncLock _mutex = new AsyncLock();

        /// <summary>Create a FixedIntervalRefillStrategy.</summary>
        /// <param name="ticker">A ticker to use to measure time.</param>
        /// <param name="numTokens">The number of tokens to add to the bucket every interval.</param>
        /// <param name="period">How often to refill the bucket.</param>
        public FixedIntervalRefillStrategy(Ticker ticker, long numTokens, TimeSpan period)
        {
            _ticker = ticker;
            _numTokens = numTokens;
            _periodInTicks = period.Ticks;
            _nextRefillTime = -1;
        }

        public async Task<long> RefillAsync()
        {
            using (await _mutex.LockAsync().ConfigureAwait(false))
            {
                var now = _ticker.Read();
                if (now < _nextRefillTime)
                {
                    return 0;
                }
                var refillAmount = Math.Max((now - _nextRefillTime) / _periodInTicks, 1);
                _nextRefillTime += _periodInTicks * refillAmount;
                return _numTokens * refillAmount;
            }
        }
    }
}
