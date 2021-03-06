﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thingy.GraphicsPlusGui
{
    public class RectangleElement : BaseElement
    {
        public override void Draw(Graphics graphics)
        {
            graphics.FillRectangles(StandardSolidBrush, new RectangleF[] { StandardRectangleF });
        }
    }
}
