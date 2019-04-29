using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;

namespace WindowsApplication14
{
    public partial class Form1 : Form
    {
        SerialPort serialPort;
        Thread dataInThread; //create a thread for continuous data capture
        private System.Windows.Forms.Timer timer1;

        string pixelValueString;
        float[] pixelValues = new float[64];

        private int counter;

        // delegate is used to write to a UI control from a non-UI thread
        private delegate void SetTextDeleg(string text);
        //private delegate void DrawHeatMap(Graphics g);

        public Form1()
        {
            InitializeComponent();
            this.BackColor = Color.White;
            this.Width = 1600;
            this.Height = 1000;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            serialPort = new SerialPort("COM3", 115200, Parity.None, 8, StopBits.One);
            serialPort.Handshake = Handshake.None;
            serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;
            serialPort.Open();

            InitTimer();

            //dataInThread = new Thread(DoThisAllTheTime);
        }

        public void InitTimer()
        {
            timer1 = new System.Windows.Forms.Timer();
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Interval = 7000; // in miliseconds
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            serialPort.Write("1");
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            DrawObjects(g);
            g.Dispose();
        }

        private void DrawObjects(Graphics g)
        {
            ColorMap cm = new ColorMap();
            Random rand = new Random();
            int[,] cmap = cm.Jet();
            int x0 = 10;
            int y0 = 10;
            int width = 85;
            int height = 85;
            PointC[,] pts = new PointC[8, 8];
            int p = 0;

            //Remap float values to uint8 range 0-255
            for (int i = 0; i < 64; i++){
                pixelValues[i] = Remap(pixelValues[i],22,27,0,255);
            }

            //Create new points used for interpolation
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    pts[i, j] = new PointC(new PointF(x0 + width*j, y0 + height*i), pixelValues[p]);
                    p += 1;
                }
            }
           
            float cmin = 0;
            float cmax = 255;
            int colorLength = cmap.GetLength(0);

            // Create original pixelated color map:
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int cindex = (int)Math.Round((colorLength * (pts[i, j].C - cmin) +
                        (cmax - pts[i, j].C)) / (cmax - cmin));
                    if (cindex < 1)
                        cindex = 1;
                    if (cindex > colorLength)
                        cindex = colorLength;
                    for (int k = 0; k < 4; k++)
                    {
                        pts[i, j].ARGBArray[k] = cmap[cindex - 1, k];
                    }
                }
            }

            //Draw the image
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    SolidBrush aBrush = new SolidBrush(Color.FromArgb(pts[i, j].ARGBArray[0],
                        pts[i, j].ARGBArray[1], pts[i, j].ARGBArray[2], pts[i, j].ARGBArray[3]));
                    PointF[] pta = new PointF[4]{pts[i,j].pointf, pts[i+1,j].pointf,
                        pts[i+1,j+1].pointf, pts[i,j+1].pointf};
                    g.FillPolygon(aBrush, pta);
                    aBrush.Dispose();
                }
            }
            g.DrawString("Direct Color Map", this.Font, Brushes.Black, 50, 190);

            // Perform Bilinear interpolation:

            //Create new points shifted over to the right of first drawing
            x0 = x0 + 700; 
            p = 0;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    pts[i, j] = new PointC(new PointF(x0 + width * j, y0 + height * i), pixelValues[p]);
                    p += 1;
                }
            }
            //Draw new image
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    PointF[] pta = new PointF[4]{pts[i,j].pointf, pts[i+1,j].pointf,
                        pts[i+1,j+1].pointf, pts[i,j+1].pointf};
                    float[] cdata = new float[4]{pts[i,j].C,pts[i+1,j].C,
                        pts[i+1,j+1].C,pts[i,j+1].C};
                    Interp(g, pta, cdata, 125);
                }
            }
            g.DrawString("Interpolated Color Map", this.Font, Brushes.Black, 740, 190);
        }

        private void Interp(Graphics g, PointF[] pta, float[] cData, int npoints)
        {
            PointC[,] pts = new PointC[npoints + 1, npoints + 1];
            float x0 = pta[0].X;
            float x1 = pta[3].X;
            float y0 = pta[0].Y;
            float y1 = pta[1].Y;
            float dx = (x1 - x0) / npoints;
            float dy = (y1 - y0) / npoints;
            float C00 = cData[0];
            float C10 = cData[1];
            float C11 = cData[2];
            float C01 = cData[3];

            for (int i = 0; i <= npoints; i++)
            {
                float x = x0 + i * dx;
                for (int j = 0; j <= npoints; j++)
                {
                    float y = y0 + j * dy;
                    float C = (y1 - y) * ((x1 - x) * C00 +
                                     (x - x0) * C10) / (x1 - x0) / (y1 - y0) +
                                     (y - y0) * ((x1 - x) * C01 +
                                     (x - x0) * C11) / (x1 - x0) / (y1 - y0);
                    pts[j, i] = new PointC(new PointF(x, y), C);
                }
            }

            ColorMap cm = new ColorMap();
            int[,] cmap = cm.Jet();
            float cmin = 0;           float cmax = 255;
            int colorLength = cmap.GetLength(0);
            for (int i = 0; i <= npoints; i++)
            {
                for (int j = 0; j <= npoints; j++)
                {
                    int cindex = (int)Math.Round((colorLength * (pts[i, j].C - cmin) +
                        (cmax - pts[i, j].C)) / (cmax - cmin));
                    if (cindex < 1)
                        cindex = 1;
                    if (cindex > colorLength)
                        cindex = colorLength;
                    for (int k = 0; k < 4; k++)
                    {
                        pts[j, i].ARGBArray[k] = cmap[cindex - 1, k];
                    }
                }
            }

            for (int i = 0; i < npoints; i++)
            {
                for (int j = 0; j < npoints; j++)
                {
                    SolidBrush aBrush = new SolidBrush(Color.FromArgb(pts[i, j].ARGBArray[0],
                        pts[i, j].ARGBArray[1], pts[i, j].ARGBArray[2], pts[i, j].ARGBArray[3]));
                    PointF[] points = new PointF[4]{pts[i,j].pointf, pts[i+1,j].pointf,
                        pts[i+1,j+1].pointf, pts[i,j+1].pointf};
                    g.FillPolygon(aBrush, points);
                    aBrush.Dispose();
                }
            }
        }

        //public void DoThisAllTheTime()
        //{
        //    try
        //    {
        //        while (true)
        //        {
        //            //you need to use Invoke because the new thread can't access the UI elements directly
        //            MethodInvoker mi = delegate ()
        //            {
        //                this.Text = DateTime.Now.ToString();
        //                serialPort.Write("1");
        //                Thread.Sleep(5000);
        //            };
        //            this.Invoke(mi);
        //            //Thread.Sleep(1000);
        //        }
        //    }
        //    catch (ThreadAbortException abortException)
        //    {
        //        Console.WriteLine((string)abortException.ExceptionState);
        //    }
        //}

        private void button1_Click(object sender, EventArgs e)
        {
            Thread.Sleep(2000);
            //dataInThread.Abort();
        }

        private void btnStart_Click_1(object sender, EventArgs e)
        {
            // Makes sure serial port is open before trying to write
            try
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                }

                //dataInThread.Start();
                //serialPort.Write("1");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening/writing to serial port :: " + ex.Message, "Error!");
            }
        }

        void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
            }

            Thread.Sleep(2000);
            dataInThread.Abort();
        }

        void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] highByte = new byte[64];
            byte[] lowByte = new byte[64];
            //string pixelValueString;
            //float[] pixelValues;

            Thread.Sleep(500);

            //Read high and low bytes from serial port
            serialPort.Read(highByte, 0, 64);
            serialPort.Read(lowByte, 0, 64);

            pixelValueString = bytesToString(highByte, lowByte);
            pixelValues = bytesToFloat(highByte, lowByte);

            //Have to use invoke as we are calling the method from a non-primary thread
            this.BeginInvoke(new SetTextDeleg(si_DataReceived), new object[] { pixelValueString });
            //this.BeginInvoke(new DrawHeatMap(DrawObjects), new object[] { Graphics g});

        }

        string bytesToString(byte[] highByte, byte[] lowByte)
        {
            int numBytesSent = highByte.Length;
            UInt16 integerPixelVal = 0;
            var pixel = new float[numBytesSent];

            string data = "";
            string pixelDat = "";

            //Convert from bytes back to float values
            for (int i = 1; i <= numBytesSent; i++)
            {
                integerPixelVal = (UInt16)(highByte[i - 1] * 256 + lowByte[i - 1]);
                pixel[i - 1] = (float)(integerPixelVal / 100.0);

                pixelDat = pixel[i - 1].ToString();
                data = data + " " + pixelDat;
                if (i % 8 == 0) { data = data + "\n"; }
            }

            return data;
        }

        float[] bytesToFloat(byte[] highByte, byte[] lowByte)
        {
            int numBytesSent = highByte.Length;
            UInt16 integerPixelVal = 0;
            var pixel = new float[numBytesSent];

            //Convert from bytes back to float values
            for (int i = 0; i < numBytesSent; i++)
            {
                integerPixelVal = (UInt16)(highByte[i] * 256 + lowByte[i]);
                pixel[i] = (float)(integerPixelVal / 100.0); 
            }

            return pixel;
        }

        float Remap(float from, float fromMin, float fromMax, int toMin, int toMax)
        {
            var fromAbs = from - fromMin;
            var fromMaxAbs = fromMax - fromMin;

            var normal = fromAbs / fromMaxAbs;

            var toMaxAbs = toMax - toMin;
            var toAbs = toMaxAbs * normal;

            var to = toAbs + toMin;

            return to;
        }

        private void si_DataReceived(string data)
        {
            //richTextBox1.Text = data.Trim(); // test with larger textbox
            Invalidate();
        }

    }
}