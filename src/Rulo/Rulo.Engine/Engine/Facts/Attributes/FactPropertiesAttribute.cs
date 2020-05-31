using System;

namespace Rulo.Engine.Facts.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class FactPropertiesAttribute : Attribute
    {
        public string FactId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public FactSourceActivationPolicy ActivationPolicy { get; set; }
    }
}
