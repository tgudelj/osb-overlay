using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSB
{
    public class OSBButtonTag
    {
        public OSBButtonTag()
        {
        }

        public OSBButtonTag(int vJoyButtonId, int buttonIndex)
        {
            this.vJoyButtonId = vJoyButtonId;
            ButtonIndex = buttonIndex;
        }

        public int vJoyButtonId { get; set; }
        public int ButtonIndex { get; set; }
    }
}
