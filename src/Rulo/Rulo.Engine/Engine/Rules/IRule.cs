using Rulo.Engine.Conditions;

namespace Rulo.Engine.Rules
{
    public interface IRule
    {
        int Priority { get; }

        Condition Condition { get; }
    }
}
