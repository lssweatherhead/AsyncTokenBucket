using System.Threading;
using System.Threading.Tasks;

namespace Banda.AsyncTokenBucket
{
    public interface ITokenBucket
    {
        /// <summary>
        /// Attempt to consume a single token from the bucket.  If it was consumed then <code>true</code>
        /// is returned, otherwise <code>false</code> is returned.
        /// </summary>
        /// <returns><code>true</code> if the tokens were consumed, <code>false</code> otherwise.</returns>
        Task<bool> TryConsumeAsync();

        /// <summary>
        /// Attempt to consume a specified number of tokens from the bucket.  If the tokens were consumed then <code>true</code>
        /// is returned, otherwise <code>false</code> is returned.
        /// </summary>
        /// <param name="numTokens">The number of tokens to consume from the bucket, must be a positive number.</param>
        /// <returns><code>true</code> if the tokens were consumed, <code>false</code> otherwise.</returns>
        Task<bool> TryConsumeAsync(long numTokens);

        /// <summary>
        /// Consume a single token from the bucket.  If no token is currently available then this method will block until a
        /// token becomes available.
        /// </summary>
        Task WaitConsumeAsync();

        /// <summary>
        /// Consumes multiple tokens from the bucket.  If enough tokens are not currently available then this method will block
        /// </summary>
        /// <param name="numTokens">The number of tokens to consume from the bucket, must be a positive number.</param>
        Task WaitConsumeAsync(long numTokens);

        Task WaitConsumeAsync(long numTokens, CancellationToken cancellationToken);

        bool HasSlept();
    }
}
