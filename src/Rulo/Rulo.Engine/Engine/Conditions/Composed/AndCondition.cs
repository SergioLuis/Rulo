using System.Threading.Tasks;

namespace Rulo.Engine.Conditions.Composed
{
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
}
