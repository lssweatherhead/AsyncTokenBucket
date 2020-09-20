using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Banda.AsyncTokenBucket.Tests
{
    public class FixedIntervalRefillStrategyTest
    {
        private const long NumberOfTokens = 5;
        private readonly TimeSpan _period = TimeSpan.FromSeconds(10);

        private MockTicker _ticker;
        private FixedIntervalRefillStrategy _strategy;

        [SetUp]
        public void SetUp()
        {
            _ticker = new MockTicker();
            _strategy = new FixedIntervalRefillStrategy(_ticker, NumberOfTokens, _period);
        }

        [Test]
        public async Task FirstRefill()
        {
            Assert.AreEqual(NumberOfTokens, await _strategy.RefillAsync().ConfigureAwait(false));
        }

        [Test]
        public async Task NoRefillUntilPeriodUp()
        {
            await _strategy.RefillAsync().ConfigureAwait(false);

            // Another refill shouldn't come for P units.
            for (var i = 0; i < _period.TotalSeconds - 1; i++)
            {
                _ticker.Advance(TimeSpan.FromSeconds(1));
                Assert.AreEqual(0, await _strategy.RefillAsync().ConfigureAwait(false));
            }
        }

        [Test]
        public async Task RefillEveryPeriod()
        {
            for (var i = 0; i < 10; i++)
            {
                Assert.AreEqual(NumberOfTokens, await _strategy.RefillAsync().ConfigureAwait(false));
                _ticker.Advance(_period);
            }
        }

        [Test]
        public async Task RefillMultipleTokensWhenMultiplePeriodsElapse()
        {
            _ticker.Advance(TimeSpan.FromSeconds(_period.TotalSeconds * 3));
            Assert.That(await _strategy.RefillAsync().ConfigureAwait(false), Is.EqualTo(NumberOfTokens * 3));

            _ticker.Advance(_period);
            Assert.That(await _strategy.RefillAsync().ConfigureAwait(false), Is.EqualTo(NumberOfTokens));
        }

        [Test]
        public async Task RefillAtFixedRateWhenCalledWithInconsistentRate()
        {
            _ticker.Advance(TimeSpan.FromSeconds(_period.TotalSeconds / 2));
            Assert.That(await _strategy.RefillAsync().ConfigureAwait(false), Is.EqualTo(NumberOfTokens));

            _ticker.Advance(TimeSpan.FromSeconds(_period.TotalSeconds / 2));
            Assert.That(await _strategy.RefillAsync().ConfigureAwait(false), Is.EqualTo(NumberOfTokens));
        }

        private sealed class MockTicker : Ticker
        {
            private long _now;

            public override long Read()
            {
                return _now;
            }

            public void Advance(TimeSpan delta)
            {
                _now += delta.Ticks;
            }
        }
    }
}
