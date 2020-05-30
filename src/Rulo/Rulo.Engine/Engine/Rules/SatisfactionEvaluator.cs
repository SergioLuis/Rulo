using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rulo.Engine.Conditions;
using Rulo.Engine.Facts;

namespace Rulo.Engine.Engine.Rules
{
    public class SatisfactionEvaluator
    {
        public SatisfactionEvaluator(FactSourceContainer factSourceContainer)
        {
            mFactSourceContainer = factSourceContainer;
            mFactContainer = new FactContainer();
        }

        public async Task<SatisfactionStatus> EvaluateCondition(Condition condition)
        {
            List<Task<Fact>> tasks = new List<Task<Fact>>();
            IEnumerable<string> requiredFactIds = condition.GetRequiredFactIds();

            List<Task<Fact>> requestFactTasks = requiredFactIds
                .Where(fact => !mFactContainer.IsCached(fact, EngineClock.Default.Now()))
                .Select(fact => mFactSourceContainer.RequestNonGenericFact(fact))
                .ToList();

            await Task.WhenAll(tasks);

            List<Fact> generatedFacts = requestFactTasks
                .Select(task => task.Result)
                .ToList();

            mFactContainer.AddFactRange(generatedFacts);

            SatisfactionStatus result = SatisfactionStatus.Unknown;
            using (EvaluationContext ctx = condition.StartEvaluation(mFactContainer))
            {
                result = await ctx.Evaluate();
            }

            if ((result & SatisfactionStatus.Unknown) != SatisfactionStatus.Unknown)
                return result;

            throw new Exception(
                $"Got a SatisfactionStatus.Unknown after finishing evaluation!");
        }

        readonly FactSourceContainer mFactSourceContainer;
        readonly FactContainer mFactContainer;
    }
}
