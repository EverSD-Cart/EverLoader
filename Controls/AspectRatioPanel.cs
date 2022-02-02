using EverLoader.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Windows.Forms;

namespace EverLoader.Controls
{
    public class AspectRatioPanel : Panel
    {
        private Size _startSize = Size.Empty;
        private Dictionary<string, SizeAndRatio> _childOrigSizes;

        //public AspectRatioPanel() : base() {}

        public new void PerformLayout()
        {
            base.PerformLayout();

            _startSize = this.Size;
            _childOrigSizes = new Dictionary<string, SizeAndRatio>();
            foreach (var c in Controls.Cast<Control>().Where(c => c is PictureBox))
            {
                _childOrigSizes.Add(c.Name, new SizeAndRatio(c.Size));
            }

            this.SizeChanged += AspectRatioPanel_SizeChanged;
        }

        private void AspectRatioPanel_SizeChanged(object sender, EventArgs e)
        {
            int deltaWidth = this.Width - _startSize.Width;
            int deltaHeight = this.Height - _startSize.Height;
            if (deltaWidth < 0 || deltaHeight < 0) return;

            double deltaRatio = (double)deltaWidth / deltaHeight;

            foreach (var p in Controls.Cast<Control>().Where(c => c is PictureBox))
            {
                var childSizeAndRatio = _childOrigSizes[p.Name];

                int newWidth, newHeight;

                if (deltaRatio >= childSizeAndRatio.Ratio)
                {
                    //panel resized wider than child
                    newHeight = Math.Max(0, childSizeAndRatio.Size.Height + deltaHeight);
                    newWidth = (int)(newHeight * childSizeAndRatio.Ratio);
                }
                else
                {
                    //panel resized taller than child
                    newWidth = Math.Max(0, childSizeAndRatio.Size.Width + deltaWidth);
                    newHeight = (int)(newWidth / childSizeAndRatio.Ratio);
                }

                p.Size = new Size(newWidth, newHeight);
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.Layout -= AspectRatioPanel_SizeChanged;
            base.Dispose(disposing);
        }
    }
}
