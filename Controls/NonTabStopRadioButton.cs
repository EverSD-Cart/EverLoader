using System;
using System.Windows.Forms;

namespace EverLoader.Controls
{
    public class NonTabStopRadioButton : RadioButton
    {
        protected override void OnTabStopChanged(EventArgs e)
        {
            base.OnTabStopChanged(e);

            if (TabStop) TabStop = false;
        }
    }
}
