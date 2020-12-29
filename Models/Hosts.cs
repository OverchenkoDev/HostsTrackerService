using System;
using System.Collections.Generic;

namespace HostTracker.Models
{
    public partial class Hosts
    {
        public int Id { get; set; }
        public string ServiceName { get; set; }
        public string CheckDomain { get; set; }
        public string NotificationUrl { get; set; }
    }
}
