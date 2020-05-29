using System;
using System.Threading.Tasks;

namespace Rulo.Engine.Facts
{
    public class Result<T>
    {
        public T Data { get; private set; }
        public TimeSpan TimeToLive { get; private set; }

        public Result(T data, TimeSpan timeToLive)
        {
            Data = data;
            TimeToLive = timeToLive;
        }
    }

    public abstract class FactSource
    {

    }

    public abstract class FactSource<T> : FactSource
    {
        public abstract Task<Result<T>> GetFact();
    }
}
