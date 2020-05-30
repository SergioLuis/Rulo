using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rulo.Engine.Facts;
using Rulo.Engine.Engine.Conditions.Attributes;

namespace Rulo.Engine.Conditions
{
    public abstract class Condition
    {
        public virtual IEnumerable<string> GetRequiredFactIds()
        {
            if (mRequiredFactIds != null)
                return mRequiredFactIds;

            Type type = this.GetType();
            ConditionPropertiesAttribute[] attrs = type.GetCustomAttributes( 
                typeof(ConditionPropertiesAttribute),
                true) as ConditionPropertiesAttribute[];

            if (attrs == null || attrs.Length == 0)
            {
                throw new Exception(
                    $"Condition {this.GetType()} is not decorated with a ConditionPropertiesAttribute.");
            }

            HashSet<string> result = new HashSet<string>();
            foreach (ConditionPropertiesAttribute attr in attrs)
                result.Add(attr.FactId);

            mRequiredFactIds = result.ToList().AsReadOnly();
            return mRequiredFactIds;
        }

        internal abstract EvaluationContext StartEvaluation(FactContainer container);

        internal abstract void FinishEvaluation();

        public abstract Task<SatisfactionStatus> GetSatisfactionStatus();

        protected IEnumerable<string> mRequiredFactIds;
    }

    public abstract class Condition<T> : Condition
    {
        internal override EvaluationContext StartEvaluation(FactContainer container)
        {
            string factId = GetRequiredFactIds().FirstOrDefault();
            if (string.IsNullOrEmpty(factId))
                return new EvaluationContext(this);

            SetFact(container.PullFact<T>(factId));
            return new EvaluationContext(this);
        }

        internal override void FinishEvaluation() => CleanFact();

        internal void SetFact(Fact<T> fact) => FactToCheck = fact;

        internal void CleanFact() => FactToCheck = default(Fact<T>);

        protected bool HasFactToCheck { get => FactToCheck != null; }
        protected Fact<T> FactToCheck;
    }
}
