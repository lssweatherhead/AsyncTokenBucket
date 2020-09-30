using System;
using System.Threading.Tasks;

namespace Banda.AsyncTokenBucket
{
    public static class TokenBuckets
    {
        private static readonly Ticker _ticker = Ticker.Default();

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async Task<ITokenBucket> BucketWithFixedIntervalRefillStrategy
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            (long capacity, long refillTokens, TimeSpan period)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException("capacity", "Must specify a positive number of tokens");
            if (refillTokens <= 0)
                throw new ArgumentOutOfRangeException("refillTokens", "Must specify a positive number of tokens");
            if (period == null)
                throw new ArgumentNullException("period", "Must specify a period");
            if (period.Duration() == TimeSpan.Zero)
                throw new ArgumentOutOfRangeException("period", "Must specify a non-zero period");

            period = period.Duration();

            var strategy = new FixedIntervalRefillStrategy(_ticker, refillTokens, period);

            return new TokenBucket(capacity, strategy);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async Task<ITokenBucket> BucketWithRefillStrategy
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            (long capacity, IRefillStrategy refillStrategy)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException("capacity", "Must specify a positive number of tokens");
            if (refillStrategy == null)
                throw new ArgumentNullException("refillStrategy");

            return new TokenBucket(capacity, refillStrategy);
        }
    }
}
