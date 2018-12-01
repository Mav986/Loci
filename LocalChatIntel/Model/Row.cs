using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalChatIntel
{
    class Row
    {
        public long Pilot_Id { get; set; }
        public string Pilot_Name { get; set; }
        public string Corp_Name { get; set; }
        public string Alliance_Name { get; set; } = "";
        public int Danger_Percent { get; set; }
        public int Solo_Percent { get; set; }
        public string Notes { get; set; }
    }
}
