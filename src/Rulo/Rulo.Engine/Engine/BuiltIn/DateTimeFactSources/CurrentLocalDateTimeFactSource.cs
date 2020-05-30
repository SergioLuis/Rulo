using System;
using System.Threading.Tasks;

using Rulo.Engine.Facts;
using Rulo.Engine.Facts.Attributes;

namespace Rulo.Engine.BuiltIn.DateTimeFactSources
{
    [FactProperties(
        FactId = FactIds.DateTimeFacts.CurrentLocalDateTime,
        Name = "CurrentLocalDateTime",
        Description = "Current date and time, in local format",
        ActivationPolicy = FactSourceActivationPolicy.OnEngineStartup
    )]
    public class CurrentLocalDateTimeFactSource : FactSource<DateTime>
    {
        public override Task<Result<DateTime>> GetFactResult()
        {
            return Task.FromResult(
                new Result<DateTime>(DateTime.Now.ToLocalTime(), TimeSpan.Zero));
        }
    }
}
