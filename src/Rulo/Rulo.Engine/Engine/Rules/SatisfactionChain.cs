using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Rulo.Engine.Engine.Conditions;
using Rulo.Engine.Engine.Facts;
using Rulo.Engine.Facts;

namespace Rulo.Engine.Engine.Rules
{
    public class SatisfactionChain
    {
        public SatisfactionChain(FactSourceContainer factSourceContainer)
        {
            mFactSourceContainer = factSourceContainer;
        }

        public async Task<bool> IsSatisfied(Condition condition)
        {
            Type taskType = typeof(Task<>);

            Type factType = typeof(Fact<>);
            Type conditionType = typeof(Condition);

            MethodInfo requestFactMethodInfo = typeof(FactSourceContainer)
                .GetMethod(nameof(FactSourceContainer.RequestFact));

            List<Task> tasks = new List<Task>();

            Condition.InvocationFact[] factsForInvocation =
                condition.GetFactsForInvocation();

            factsForInvocation = factsForInvocation
                .GroupBy(f => f.FactId)
                .Select(l => l.First())
                .ToArray();

            foreach (var invocationFact in factsForInvocation)
            {
                MethodInfo genericRequestFactMethod = requestFactMethodInfo
                    .MakeGenericMethod(invocationFact.FactType);

                // TODO check if the result is already cached
                tasks.Add(
                    genericRequestFactMethod.Invoke(
                        mFactSourceContainer, new[] { invocationFact.FactId }) as Task);
            }

            await Task.WhenAll(tasks);

            for (int i = 0; i < factsForInvocation.Length; i++)
            {
                Type taskResultType = taskType.MakeGenericType(
                    factType.MakeGenericType(factsForInvocation[i].FactType));

                object fact = taskResultType
                    .GetProperty(nameof(Task<object>.Result))
                    .GetValue(tasks[i], null);

                Type genericFactType = factType
                    .MakeGenericType(factsForInvocation[i].FactType);

                object factData = genericFactType
                    .GetProperty(nameof(Fact<object>.Data))
                    .GetValue(fact, null);

                string factId = genericFactType
                    .GetProperty(nameof(Fact<object>.FactId))
                    .GetValue(fact, null) as string;

                if (await condition.IsSatisfied(factId, factData))
                    return true;
            }

            return false;
        }

        readonly FactSourceContainer mFactSourceContainer;
    }
}
