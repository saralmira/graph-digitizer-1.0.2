using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphDigitizer.Models
{
    public class LineModel
    {
        public bool ScreenOrReal { get; set; }
        public bool XOrY { get; set; }
        public UInt32 Count { get; set; }
    }
}
