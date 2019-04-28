using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace WindowsApplication14
{
    public class PointC
    {
        public PointF pointf = new PointF();
        public float C = 0;
        public int[] ARGBArray = new int[4];

        public PointC()
        {

        }

        public PointC(PointF ptf, float c)
        {
            pointf = ptf;
            C = c;
        }

        public PointC(PointF ptf, float c, int[] argbArray)
        {
            pointf = ptf;
            C = c;
            ARGBArray = argbArray;
        }
    }
}
