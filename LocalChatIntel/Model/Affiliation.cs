using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalChatIntel
{
    class Affiliation
    {
        public long Character_Id { get; set; }
        public string Corporation { get; set; }
        public string Alliance { get; set; } = String.Empty;
    }
}
