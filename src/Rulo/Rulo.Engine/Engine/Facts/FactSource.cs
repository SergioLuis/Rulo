using System;
using System.Threading.Tasks;

namespace Rulo.Engine.Facts
{
    public abstract class Result
    {
        public TimeSpan TimeToLive { get; private set; }

        public Result(TimeSpan timeToLive)
        {
            TimeToLive = timeToLive;
        }
    }

    public class Result<T> : Result
    {
        public T Data { get; private set; }

        public Result(T data, TimeSpan timeToLive) : base(timeToLive)
        {
            Data = data;
        }
    }

    public abstract class FactSource
    {
        internal abstract Task<Result> GetFact();
    }

    public abstract class FactSource<T> : FactSource
    {
        internal override async Task<Result> GetFact()
        {
            return await GetFactResult();
        }

        public abstract Task<Result<T>> GetFactResult();
    }
}
