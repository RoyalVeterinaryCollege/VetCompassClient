using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsExample
{
    public static class WinformsHelper
    {
        public static void Invoke(this Control control, Action action)
        {
            control.Invoke(action);
        }
    }
}
