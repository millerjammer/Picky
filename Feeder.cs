using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    internal class Feeder
    {
        public Part part { get; set; }
        public double width { get; set; }
        public double thickness { get; set; }
        public double interval { get; set; }

        public Feeder()
        {
        }
    }
}
