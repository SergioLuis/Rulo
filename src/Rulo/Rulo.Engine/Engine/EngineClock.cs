using System;
namespace Rulo.Engine.Engine
{
    public interface IEngineClock
    {
        DateTime Now();
        DateTime UtcNow();
    }

    public class EngineClock : IEngineClock
    {
        DateTime IEngineClock.Now()
        {
            return DateTime.Now;
        }

        DateTime IEngineClock.UtcNow()
        {
            return DateTime.UtcNow;
        }

        public static readonly IEngineClock Default = new EngineClock();
    }
}
