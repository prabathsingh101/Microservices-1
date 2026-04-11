using System;

namespace Identity.API.Models
{
    public class SystemLogDto
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string Level { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public string Exception { get; set; }
        public string ServiceName { get; set; }
        public string CorrelationId { get; set; }
    }
}
