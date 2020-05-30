using System.Threading.Tasks;

namespace Rulo.Engine.Conditions.Composed
{
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
