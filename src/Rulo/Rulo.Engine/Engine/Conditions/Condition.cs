using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rulo.Engine.Facts;
using Rulo.Engine.Engine.Conditions.Attributes;

namespace Rulo.Engine.Conditions
{
    [Flags]
    public enum SatisfactionStatus : byte
    {
        Unknown = 0,
        Satisfied = 1 << 0,
        Failed = 1 << 1
    }

    public class EvaluationContext : IDisposable
    {
        internal EvaluationContext(Condition condition)
        {
            mCondition = condition;
        }

        internal EvaluationContext(
            Condition condition,
            IEnumerable<EvaluationContext> nestedContext)
        {
            mCondition = condition;
            mNestedContext= nestedContext.ToList();
        }

        public async Task<SatisfactionStatus> Evaluate()
        {
            if (mCondition == null)
                throw new NullReferenceException("mCondition");

            return await mCondition.GetSatisfactionStatus();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!mbIsDisposed)
            {
                if (disposing)
                {
                    mCondition.FinishEvaluation();

                    if (mNestedContext != null)
                        mNestedContext.ToList().ForEach(m => m.Dispose(disposing));
                }

                mbIsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        readonly Condition mCondition;
        readonly List<EvaluationContext> mNestedContext;
        bool mbIsDisposed;
    }

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

        internal override void FinishEvaluation()
        {
            CleanFact();
        }

        internal void SetFact(Fact<T> fact)
        {
            FactToCheck = fact;
            HasFactToCheck = true;
        }

        internal void CleanFact()
        {
            HasFactToCheck = false;
            FactToCheck = default(Fact<T>);
        }

        protected bool HasFactToCheck;
        protected Fact<T> FactToCheck;
    }

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

    public class AndCondition : ComposedCondition
    {
        public AndCondition(params Condition[] nestedConditions)
            : base(nestedConditions) { }

        public override async Task<SatisfactionStatus> GetSatisfactionStatus()
        {
            SatisfactionStatus status = SatisfactionStatus.Unknown;
            foreach (Condition condition in mChildrenConditions)
            {
                status |= await condition.GetSatisfactionStatus();
                if ((status & SatisfactionStatus.Failed) == SatisfactionStatus.Failed)
                    return SatisfactionStatus.Failed;
            }

            return status;
        }
    }

    public class OrCondition : ComposedCondition
    {
        public OrCondition(params Condition[] nestedConditions)
            : base(nestedConditions) { }

        public override async Task<SatisfactionStatus> GetSatisfactionStatus()
        {
            SatisfactionStatus status = SatisfactionStatus.Unknown;
            foreach (Condition condition in mChildrenConditions)
            {
                status |= await condition.GetSatisfactionStatus();
                if ((status & SatisfactionStatus.Satisfied) == SatisfactionStatus.Satisfied)
                    return SatisfactionStatus.Satisfied;
            }

            return status;
        }
    }
}
