using System;
using System.Collections.Generic;

namespace HostTracker.Models
{
    public partial class EventLog
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public string Type { get; set; }
        public DateTime DateTime { get; set; }
    }
}
