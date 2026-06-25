using System;

namespace Company.Domain.Entities
{
    public class Configuration
    {
        public int Id { get; set; }
        public string ConfigKey { get; set; } = null!;
        public string ConfigValue { get; set; } = null!;
        public bool IsActive { get; set; } = true;
    }
}
