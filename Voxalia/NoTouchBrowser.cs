using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gecko;

namespace VoxaliaBrowser
{
    public class NoTouchBrowser : GeckoWebBrowser
    {
        public override bool PreProcessMessage(ref Message msg)
        {
            return true;
        }
    }
}
