using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rulo.Engine.Conditions
{
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

        bool mbIsDisposed;
        readonly Condition mCondition;
        readonly List<EvaluationContext> mNestedContext;
    }
}
