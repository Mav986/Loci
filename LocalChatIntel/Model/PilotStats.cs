using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalChatIntel
{
    class PilotStats
    {
        public long PilotId { get; set; }
        public bool SuperPilot { get; set; }
        public bool CapitalPilot { get; set; }
        public int DangerPercent { get; set; }
        public int SoloPercent { get; set; }
        public string Notes { get; set; } = "";
    }
}
