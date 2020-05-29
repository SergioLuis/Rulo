using System;
namespace Rulo.Engine.Engine.Conditions.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ConditionPropertiesAttribute : Attribute
    {
        public string FactId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Type FactType { get; set; }
    }
}
