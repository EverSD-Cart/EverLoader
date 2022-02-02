using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace EverLoader.Extensions
{
    public static class ControlExtensions
    {
        public static void AddSingle(this ControlBindingsCollection bindings, string propertyName, object dataSource, string dataMember)
        {
            bindings.Clear();
            if (dataSource != null)
            {
                bindings.Control.Enabled = true;
                bindings.Add(propertyName, dataSource, dataMember);
            }
            else
            {
                bindings.Control.Text = null;
                bindings.Control.Enabled = false;
            }
        }
    }
}
