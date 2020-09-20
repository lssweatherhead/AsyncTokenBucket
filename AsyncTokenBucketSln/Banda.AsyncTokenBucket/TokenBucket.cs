using Nito.AsyncEx;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Banda.AsyncTokenBucket
{
    internal class TokenBucket : ITokenBucket
    {
        private readonly long _capacity;
        private readonly IRefillStrategy _refillStrategy;
        private long _size;
        private readonly AsyncLock _mutex = new AsyncLock();
        private const int DelayOnSleepMs = 5; 
        private readonly static TimeSpan MaximumDelay = TimeSpan.FromMinutes(15);

        public bool Slept { get; private set; } = false;

        public TokenBucket(long capacity, IRefillStrategy refillStrategy)
        {
            _capacity = capacity;
            _refillStrategy = refillStrategy;
            _size = 0;
        }

        public async Task<bool> TryConsumeAsync()
        {
            return await TryConsumeAsync(1).ConfigureAwait(false);
        }

        public async Task<bool> TryConsumeAsync(long numTokens)
        {
            if (numTokens <= 0)
                throw new ArgumentOutOfRangeException("numTokens", "Number of tokens to consume must be positive");
            if (numTokens > _capacity)
                throw new ArgumentOutOfRangeException("numTokens", "Number of tokens to consume must be less than the capacity of the bucket.");

            using (await _mutex.LockAsync().ConfigureAwait(false))
            {
                // Give the refill strategy a chance to add tokens if it needs to, but beware of overflow
                long refilledTokens = await _refillStrategy.RefillAsync().ConfigureAwait(false);
                var newTokens = Math.Min(_capacity, Math.Max(0, refilledTokens));
                _size = Math.Max(0, Math.Min(_size + newTokens, _capacity));

                if (numTokens > _size) return false;

                // Now try to consume some tokens
                _size -= numTokens;
                return true;
            }
        }

        public async Task WaitConsumeAsync()
        {
            await WaitConsumeAsync(1).ConfigureAwait(false);
        }

        public async Task WaitConsumeAsync(long numTokens)
        {
            await WaitConsumeAsync(numTokens, null).ConfigureAwait(false);
        }

        public async Task WaitConsumeAsync(long numTokens, CancellationToken cancellationToken)
        {
            CancellationToken? ct = cancellationToken;

            await WaitConsumeAsync(numTokens, ct).ConfigureAwait(false);
        }

        private async Task WaitConsumeAsync(long numTokens, CancellationToken? cancellationToken)
        {
            long ticksOnEnteringDelayLoop = System.DateTime.UtcNow.Ticks;

            while (true)
            {
                if (await TryConsumeAsync(numTokens).ConfigureAwait(false))
                {
                    break;
                }

                if (cancellationToken.HasValue)
                {
                    await Task.Delay(DelayOnSleepMs, cancellationToken.Value).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(DelayOnSleepMs).ConfigureAwait(false);
                }

                Slept = true;

                long ticksNow = System.DateTime.UtcNow.Ticks;

                TimeSpan ticksElapsed = TimeSpan.FromTicks(ticksNow - ticksOnEnteringDelayLoop);

                if (ticksElapsed.Duration() > MaximumDelay.Duration())
                    throw new InvalidOperationException("Delay exceeded maximum delay");

                if (cancellationToken.HasValue && cancellationToken.Value.IsCancellationRequested)
                    throw new TaskCanceledException();
            }
        }


        public bool HasSlept()
        {
            return this.Slept;
        }
    }
}
