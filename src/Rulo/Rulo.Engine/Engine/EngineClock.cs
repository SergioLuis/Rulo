using System;
namespace Rulo.Engine
{
    internal interface IInternalEngineClock
    {
        void Update();
    }

    public interface IEngineClock
    {
        DateTime Now();
        DateTime UtcNow();
    }

    public class TestEngineClock : IEngineClock, IInternalEngineClock
    {
        public void Set(DateTime now)
        {
            mCurrentLocalDateTime = now.ToLocalTime();
            mCurrentUtcDateTime = now.ToUniversalTime();
        }

        DateTime IEngineClock.Now()
        {
            return mCurrentLocalDateTime;
        }

        DateTime IEngineClock.UtcNow()
        {
            return mCurrentUtcDateTime;
        }

        void IInternalEngineClock.Update()
        {
            // Nothing to do
        }

        DateTime mCurrentLocalDateTime;
        DateTime mCurrentUtcDateTime;
    }

    public class DefaultEngineClock : IEngineClock, IInternalEngineClock
    {
        void IInternalEngineClock.Update()
        {
            DateTime now = DateTime.Now;
            mCurrentLocalDateTime = now.ToLocalTime();
            mCurrentUtcDateTime = now.ToUniversalTime();
        }

        DateTime IEngineClock.Now()
        {
            return mCurrentLocalDateTime;
        }

        DateTime IEngineClock.UtcNow()
        {
            return mCurrentUtcDateTime;
        }

        DateTime mCurrentLocalDateTime;
        DateTime mCurrentUtcDateTime;
    }

    public static class EngineClock
    {
        public static IEngineClock Default
        {
            get
            {
                if (_Default == null)
                    _Default = new DefaultEngineClock();

                return _Default;
            }
            set
            {
                _Default = value;
            }
        }

        static IEngineClock _Default;
    }
}
