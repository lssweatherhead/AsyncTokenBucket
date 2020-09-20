using System.Threading.Tasks;

namespace Banda.AsyncTokenBucket
{
    public interface IRefillStrategy
    {
        /// <summary>Returns the number of tokens to add to the token bucket.</summary>
        /// <returns>The number of tokens to add to the token bucket.</returns>
        Task<long> RefillAsync();
    }
}
