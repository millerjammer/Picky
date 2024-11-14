using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Picky
{
    public class ImageProcessingStyle
    {
        public string processingName { get; set; }
        public ImageProcessingStyle(string name)
        {
            processingName = name;
        }
    }
}
