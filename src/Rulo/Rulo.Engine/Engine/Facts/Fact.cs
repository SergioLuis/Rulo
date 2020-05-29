using System;

namespace Rulo.Engine.Facts
{
    public class Fact
    {
        public string FactId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime GeneratedOn { get; set; }
        public DateTime ValidUntil { get; set; }
    }

    public class Fact<T> : Fact
    {
        public T Data { get; set; }
    }
}
