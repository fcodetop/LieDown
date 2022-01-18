using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LieDown
{
    public static class ControlExtentions
    {
        public static void SetText(this Control control, string text)
        {
            if (control.InvokeRequired)
                control.Invoke(() => control.Text = text);
            else
                control.Text = text;
        }
    }
}
