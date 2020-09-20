using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Banda.AsyncTokenBucket.Tests
{
    [TestFixture]
    public class TokenBucketRefillTests
    {
        [Test, Explicit("Long Running")]
        public async Task RateLimitTests()
        {
            const int totalConsumes = 500;
            const int refillRate = 40;

            //initial capacity 40
            //add 1 token to the bucket ~ every 25ms
            var tokenBucket = await TokenBuckets.BucketWithFixedIntervalRefillStrategy(refillRate, 1, TimeSpan.FromMilliseconds(1000d / refillRate))
                .ConfigureAwait(false);

            var sw = new Stopwatch();
            sw.Start();

            for (var i = 0; i < totalConsumes; i++)
            {
                await tokenBucket.WaitConsumeAsync().ConfigureAwait(false);
            }

            sw.Stop();

            // tokens consumed / time = refillRate
            Assert.That(totalConsumes / (sw.Elapsed.TotalSeconds + 1), Is.EqualTo(refillRate).Within(0.2));
        }
    }
}
