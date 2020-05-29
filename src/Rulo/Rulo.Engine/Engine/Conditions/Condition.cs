using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rulo.Engine.Engine.Conditions.Attributes;

namespace Rulo.Engine.Engine.Conditions
{
    public abstract class Condition
    {
        public struct InvocationFact
        {
            public string FactId;
            public Type FactType;
        }

        public virtual InvocationFact[] GetFactsForInvocation()
        {
            if (mInvovationFacts != null)
                return mInvovationFacts;

            Type type = this.GetType();
            ConditionPropertiesAttribute[] attrs = type.GetCustomAttributes( 
                typeof(ConditionPropertiesAttribute),
                true) as ConditionPropertiesAttribute[];

            if (attrs == null || attrs.Length == 0)
            {
                mInvovationFacts = Array.Empty<InvocationFact>();
                return mInvovationFacts;
            }

            mInvovationFacts = attrs.Select(attr => new InvocationFact
            {
                FactId = attr.FactId,
                FactType = attr.FactType
            }).ToArray();

            return mInvovationFacts;
        }

        public virtual Task<bool> IsSatisfied(string factId, object o)
        {
            InvocationFact[] invocationFacts = GetFactsForInvocation();
            foreach (InvocationFact f in invocationFacts)
            {
                if (f.FactId == factId)
                    return IsSatisfied(o);
            }

            return Task.FromResult(false);
        }

        public abstract Task<bool> IsSatisfied(object o);

        protected InvocationFact[] mInvovationFacts;
    }

    public abstract class ComposedCondition : Condition
    {
        public ComposedCondition(params Condition[] nestedConditions)
        {
            mChildrenConditions = new List<Condition>(nestedConditions);
        }

        public override InvocationFact[] GetFactsForInvocation()
        {
            if (mInvovationFacts != null)
                return mInvovationFacts;

            List<InvocationFact> result = new List<InvocationFact>();
            for (int i = 0; i < mChildrenConditions.Count; i++)
                result.AddRange(mChildrenConditions[i].GetFactsForInvocation());

            mInvovationFacts = result.ToArray();
            return mInvovationFacts;
        }

        protected async Task<bool[]> GetNestedConditionsResults(string factId, object o)
        {
            return await Task.WhenAll(
                mChildrenConditions.Select(c => c.IsSatisfied(factId, o)));
        }

        public override Task<bool> IsSatisfied(object o)
        {
            throw new InvalidOperationException();
        }

        readonly List<Condition> mChildrenConditions;
    }

    public class AndCondition : ComposedCondition
    {
        public AndCondition(params Condition[] nestedConditions)
            : base(nestedConditions) { }

        public override async Task<bool> IsSatisfied(string factId, object o)
        {
            foreach (bool r in await GetNestedConditionsResults(factId, o))
                if (!r) return false;

            return true;
        }
    }

    public class OrCondition : ComposedCondition
    {
        public OrCondition(params Condition[] nestedConditions)
            : base(nestedConditions) { }

        public override async Task<bool> IsSatisfied(string factId, object o)
        {
            foreach (bool r in await GetNestedConditionsResults(factId, o))
                if (r) return true;

            return false;
        }
    }
}
