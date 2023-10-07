using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    internal class Cassette
    {
        public List<Feeder> feeders {  get; set; }
        public double originX { get; set; }
        public double originY { get; set; }
        public double originZ { get; set; }

        public Cassette() {
            feeders = new List<Feeder>();
        }
    }
}
