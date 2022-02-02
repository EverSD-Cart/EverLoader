using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace EverLoader.Models
{
    public class SizeAndRatio
    {
        private Size _size;
        private readonly double _ratio;
        public SizeAndRatio(Size size)
        {
            _size = size;
            _ratio = (double)size.Width / size.Height;
        }
        public double Ratio => _ratio;
        public Size Size => _size;
    }
}
