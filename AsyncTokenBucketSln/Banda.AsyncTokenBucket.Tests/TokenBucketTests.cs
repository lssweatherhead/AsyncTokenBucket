using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Banda.AsyncTokenBucket.Tests
{
    public class TokenBucketTests
    {
        private const long Capacity = 10;
        private const int ConsumeTimeout = 30000;

        private MockRefillStrategy _refillStrategy;
        private ITokenBucket _bucket;

        [SetUp]
        public async Task SetUp()
        {
            _refillStrategy = new MockRefillStrategy();
            _bucket = await TokenBuckets.BucketWithRefillStrategy(Capacity, _refillStrategy).ConfigureAwait(false);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task TryConsumeZeroTokens()
        {
            async Task Act() => await _bucket.TryConsumeAsync(0).ConfigureAwait(false);

            NUnit.Framework.Assert.That(Act, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task TryConsumeNegativeTokens()
        {
            async Task Act() => await _bucket.TryConsumeAsync(-1).ConfigureAwait(false);

            NUnit.Framework.Assert.That(Act, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task TryConsumeMoreThanCapacityTokens()
        {
            async Task Act() => await _bucket.TryConsumeAsync(100).ConfigureAwait(false);

            NUnit.Framework.Assert.That(Act, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public async Task BucketInitiallyEmpty()
        {
            NUnit.Framework.Assert.False(await _bucket.TryConsumeAsync().ConfigureAwait(false));
        }

        [Test]
        public async Task TryConsumeOneToken()
        {
            await _refillStrategy.AddToken().ConfigureAwait(false);
            NUnit.Framework.Assert.True(await _bucket.TryConsumeAsync().ConfigureAwait(false));
        }

        [Test]
        public async Task TryConsumeMoreTokensThanAreAvailable()
        {
            await _refillStrategy.AddToken().ConfigureAwait(false);
            NUnit.Framework.Assert.False(await _bucket.TryConsumeAsync(2).ConfigureAwait(false));
        }

        [Test]
        public async Task TryRefillMoreThanCapacityTokens()
        {
            await _refillStrategy.AddTokens(Capacity + 1).ConfigureAwait(false);
            NUnit.Framework.Assert.True(await _bucket.TryConsumeAsync(Capacity).ConfigureAwait(false));
            NUnit.Framework.Assert.False(await _bucket.TryConsumeAsync(1).ConfigureAwait(false));
        }

        [Test]
        public async Task TryRefillWithTooManyTokens()
        {
            await _refillStrategy.AddTokens(Capacity).ConfigureAwait(false);
            NUnit.Framework.Assert.True(await _bucket.TryConsumeAsync().ConfigureAwait(false));

            await _refillStrategy.AddTokens(long.MaxValue).ConfigureAwait(false);
            NUnit.Framework.Assert.True(await _bucket.TryConsumeAsync(Capacity).ConfigureAwait(false));
            NUnit.Framework.Assert.False(await _bucket.TryConsumeAsync(1).ConfigureAwait(false));
        }

        [Test, NUnit.Framework.Timeout(ConsumeTimeout)]
        public async Task ConsumeWhenTokenAvailable()
        {
            await _refillStrategy.AddToken().ConfigureAwait(false);
            await _bucket.WaitConsumeAsync().ConfigureAwait(false);

            NUnit.Framework.Assert.False(_bucket.HasSlept());          
        }

        [Test, NUnit.Framework.Timeout(ConsumeTimeout)]
        public async Task ConsumeWhenTokensAvailable()
        {
            const int tokensToConsume = 2;
            await _refillStrategy.AddTokens(tokensToConsume).ConfigureAwait(false);
            await _bucket.WaitConsumeAsync(tokensToConsume).ConfigureAwait(false);

            NUnit.Framework.Assert.False(_bucket.HasSlept());
        }

        [Test, NUnit.Framework.Timeout(ConsumeTimeout)]
        public async Task ConsumeWhenTokenUnavailable()
        {
            Task consumingTask = Task.Run(async () =>
            {
                await _bucket.WaitConsumeAsync(1).ConfigureAwait(false);
            });

            Thread.Sleep(3000);

            Task refillTask = Task.Run(async () =>
            {
                await _refillStrategy.AddToken().ConfigureAwait(false);
            });

            await Task.WhenAll(consumingTask, refillTask).ConfigureAwait(false);

            NUnit.Framework.Assert.True(_bucket.HasSlept());
        }

        [Test, NUnit.Framework.Timeout(ConsumeTimeout)]
        public async Task ConsumeWhenTokensUnavailable()
        {
            const int tokensToConsume = 7;

            Task consumingTask = Task.Run(async () =>
            {
                await _bucket.WaitConsumeAsync(tokensToConsume).ConfigureAwait(false);
            });

            Thread.Sleep(10000);

            Task refillTask = Task.Run(async () =>
            {
                await _refillStrategy.AddTokens(tokensToConsume).ConfigureAwait(false);
            });

            await Task.WhenAll(consumingTask, refillTask).ConfigureAwait(false);

            NUnit.Framework.Assert.True(_bucket.HasSlept());
        }

        private sealed class MockRefillStrategy : IRefillStrategy
        {
            private long _numTokensToAdd;

            public async Task AddToken()
            {
                _numTokensToAdd++;
            }

            public async Task AddTokens(long numTokens)
            {
                _numTokensToAdd += numTokens;
            }

            public async Task<long> RefillAsync()
            {
                var numTokens = _numTokensToAdd;
                _numTokensToAdd = 0;
                return numTokens;
            }
        }
    }
}
