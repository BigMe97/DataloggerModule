using System;

namespace OPCtoSQLlogger
{
    public class Device
    {
        // Added in configuration
        public string OPCAddress { get; set; }

        public string OPCTag { get; set; }

        public string MainLocation { get; set; }

        public string Location { get; set; }

        public string Equipment { get; set; }

        public string Tag { get; set; }
        
        // added when read from opc
        public double value { get; set; }

        public DateTime? lastUpdate { get; set; }

        public bool newUpdate = false;

    }
}
