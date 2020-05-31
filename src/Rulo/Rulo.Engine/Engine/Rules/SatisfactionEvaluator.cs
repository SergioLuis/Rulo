using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Rulo.Engine.Conditions;
using Rulo.Engine.Facts;
using Rulo.Engine.Rules.Attributes;

namespace Rulo.Engine.Rules
{
    public class SatisfactionEvaluator
    {
        public SatisfactionEvaluator(FactSourceContainer factSourceContainer)
        {
            mFactSourceContainer = factSourceContainer;
            mFactContainer = new FactContainer();
        }

        public async Task<RuleEvaluationResult> EvaluateRule(IRule rule)
        {
            SatisfactionStatus ruleConditionSatisfaction =
                await EvaluateCondition(rule.Condition);

            if (!IsSatisfied(ruleConditionSatisfaction))
                return RuleEvaluationResult.NotEvaluated;

            Type ruleType = rule.GetType();
            MethodInfo notAsyncFireMethodInfo = ruleType.GetMethod("Fire");
            MethodInfo asyncFireMethodInfo = ruleType.GetMethod("FireAsync");

            if (notAsyncFireMethodInfo == null && asyncFireMethodInfo == null)
            {
                throw new Exception(
                    $"{ruleType} does not implement a 'Fire' or 'FireAsync' method!");
            }

            if (notAsyncFireMethodInfo != null && asyncFireMethodInfo != null)
            {
                throw new Exception(
                    $"{ruleType} cannot implement both 'Fire' and 'FireAsync' at the same time!");
            }

            (MethodInfo fireMethodInfo, bool bIsAsync) =
                notAsyncFireMethodInfo == null
                    ? (asyncFireMethodInfo, true)
                    : (notAsyncFireMethodInfo, false);

            Type returnType = bIsAsync
                ? typeof(Task<RuleEvaluationResult>)
                : typeof(RuleEvaluationResult);

            if (!fireMethodInfo.ReturnType.Equals(returnType))
            {
                throw new Exception(
                    $"Method '{fireMethodInfo}' should return '{returnType}'!");
            }

            ParameterInfo[] fireMethodParameters = 
                notAsyncFireMethodInfo.GetParameters();

            Type factParamAttrType = typeof(FactParam);
            List<string> requiredFactIds = fireMethodParameters
                .Select(p => p.GetCustomAttribute(factParamAttrType) as FactParam)
                .Select(attr => attr.FactId)
                .ToList();

            List<Task<Fact>> requestFactTasks = requiredFactIds
                .Where(fact => !mFactContainer.IsCached(fact, EngineClock.Default.Now()))
                .Select(fact => mFactSourceContainer.RequestNonGenericFact(fact))
                .ToList();

            await Task.WhenAll(requestFactTasks);

            mFactContainer.AddFactRange(requestFactTasks.Select(t => t.Result));

            List<Fact> factsForInvocation = requiredFactIds
                .Select(fact => mFactContainer.PullFact(fact))
                .ToList();

            object[] objectsForInvocation = factsForInvocation
                .Select(f => f.GetType().GetProperty("Data").GetValue(f))
                .ToArray();

            if (bIsAsync)
            {
                Task<RuleEvaluationResult> resultTask =
                    fireMethodInfo.Invoke(rule, objectsForInvocation) as Task<RuleEvaluationResult>;
                return await resultTask;
            }

            RuleEvaluationResult? result =
                fireMethodInfo.Invoke(rule, objectsForInvocation) as RuleEvaluationResult?;

            return result.Value;
        }

        public async Task<SatisfactionStatus> EvaluateCondition(Condition condition)
        {
            IEnumerable<string> requiredFactIds = condition.GetRequiredFactIds();

            List<Task<Fact>> requestFactTasks = requiredFactIds
                .Where(fact => !mFactContainer.IsCached(fact, EngineClock.Default.Now()))
                .Select(fact => mFactSourceContainer.RequestNonGenericFact(fact))
                .ToList();

            await Task.WhenAll(requestFactTasks);

            List<Fact> generatedFacts = requestFactTasks
                .Select(task => task.Result)
                .ToList();

            mFactContainer.AddFactRange(requestFactTasks.Select(t => t.Result));

            SatisfactionStatus result = SatisfactionStatus.Unknown;
            using (EvaluationContext ctx = condition.StartEvaluation(mFactContainer))
            {
                result = await ctx.Evaluate();
            }

            if (result != SatisfactionStatus.Unknown)
                return result;

            throw new Exception(
                $"Got a SatisfactionStatus.Unknown after finishing evaluation!");
        }

        static bool IsSatisfied(SatisfactionStatus status)
            => (status & SatisfactionStatus.Satisfied) == SatisfactionStatus.Satisfied;

        readonly FactSourceContainer mFactSourceContainer;
        readonly FactContainer mFactContainer;
    }
}
