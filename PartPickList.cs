using System;using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.ComTypes;

namespace Picky
{
    internal class PartPickList
    {
        public string designator { get; set; }
        public string comment { get; set; }
        public string layer { get; set; }
        public string footprint { get; set; }
        public double x { get; set; }
        public double y { get; set; }
        public double rotation { get; set; }
        public string description { get; set; }

        public bool isPlaced { get; set; }
        public DateTime placeTime { get; set; }
        public double x_offset { get; set; }
        public double y_offset { get; set; }
        public double rotation_offset { get; set; }

        public static ObservableCollection<PartPickList> pickList;
        
        public static ObservableCollection<PartPickList> GetPickList()
        {
            pickList = new ObservableCollection<PartPickList>();
            pickList.Add(new PartPickList() { designator = "R2", comment = "10K", layer = "Top Layer", footprint = "0603", x = 1.23, y = 100.0, rotation = 90, description = "RES 10K 1% SMD 0603" });

            return pickList;
        }

        public static void test()
        {
            pickList.ElementAt(0).designator = "99";   
        }
    }
}
