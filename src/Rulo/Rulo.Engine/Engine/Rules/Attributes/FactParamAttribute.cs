using System;

namespace Rulo.Engine.Rules.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FactParam : Attribute
    {
        public string FactId { get; private set; } 

        public FactParam(string factId)
        {
            FactId = factId;
        } 
    }
}
