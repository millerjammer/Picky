using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    internal class Part
    {
        public string Designator { get; set; }
        public string Comment { get; set; }
        public string Layer { get; set; }
        public string Footprint { get; set; }
        public string CenterX { get; set; }
        public string CenterY { get; set; }
        public string Rotation { get; set; }
        public string Description { get; set; }

        public Part() { 
        }
    }
}
