using System;

namespace Rulo.Engine.Conditions
{
    [Flags]
    public enum SatisfactionStatus : byte
    {
        Unknown = 0,
        Satisfied = 1 << 0,
        Failed = 1 << 1
    }
}
