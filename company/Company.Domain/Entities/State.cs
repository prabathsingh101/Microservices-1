using System;

namespace Company.Domain.Entities
{
    public class State
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string DefaultCity { get; set; } = string.Empty;
        public string DefaultPinCode { get; set; } = string.Empty;
    }
}
