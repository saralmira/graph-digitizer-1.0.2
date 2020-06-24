using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphDigitizer.Models
{
    public class LineModel : NotifyClass
    {
        private bool _ScreenOrReal;
        private bool _XOrY;
        private UInt32 _Count;

        public bool ScreenOrReal { get { return _ScreenOrReal; } set { _ScreenOrReal = value; Notify(nameof(ScreenOrReal)); } }
        public bool XOrY { get { return _XOrY; } set { _XOrY = value; Notify(nameof(XOrY)); } }
        public UInt32 Count { get { return _Count; } set { _Count = value; Notify(nameof(Count)); } }
    }
}
