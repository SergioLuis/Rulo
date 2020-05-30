using System.Collections.Generic;
using System.Linq;

using Rulo.Engine.Facts;

namespace Rulo.Engine.Conditions.Composed
{
    public abstract class ComposedCondition : Condition
    {
        public ComposedCondition(params Condition[] nestedConditions)
        {
            mChildrenConditions = new List<Condition>(nestedConditions);
        }

        public override IEnumerable<string> GetRequiredFactIds()
        {
            if (mRequiredFactIds != null)
                return mRequiredFactIds;

            HashSet<string> result = new HashSet<string>();
            mChildrenConditions
                .Select(c => c.GetRequiredFactIds())
                .SelectMany(l => l)
                .ToList()
                .ForEach(l => result.Add(l));

            mRequiredFactIds = result.ToList().AsReadOnly();
            return mRequiredFactIds;
        }

        internal override EvaluationContext StartEvaluation(FactContainer container)
        {
            return new EvaluationContext(
                this,
                mChildrenConditions.Select(c => c.StartEvaluation(container)));
        }

        internal override void FinishEvaluation()
        {
            // Nothing to do for composed conditions
        }

        protected readonly List<Condition> mChildrenConditions;
    }
}
