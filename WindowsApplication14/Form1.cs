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
using System.Linq;
using AForge;
using AForge.Imaging;
using AForge.Imaging.ColorReduction;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;

namespace WindowsApplication14
{
    public partial class Form1 : Form
    {
        SerialPort serialPort;
        private System.Windows.Forms.Timer timer1;

        int formWidth = Screen.PrimaryScreen.Bounds.Width;
        int formHeight = Screen.PrimaryScreen.Bounds.Height;

        string audioLevelString;
        float[] pixelValues = new float[64];

        struct BlobData { public int blobCount;
                          public float[,] blobCoords; };
        bool[] seatOccupancy = { false, false, true, false };

        private delegate void SetTextDeleg(string text);

        public Form1()
        {
            InitializeComponent();
            this.BackColor = Color.White;
            this.WindowState = FormWindowState.Maximized;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            serialPort = new SerialPort("COM14", 115200, Parity.None, 8, StopBits.One);
            serialPort.Handshake = Handshake.None;
            serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;
            serialPort.Open();

            InitTimer();
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
        }

        //private void Form1_SizeChanged(object sender, EventArgs e)
        //{
        //    string formwidths = this.Width.ToString();
        //    int formwidth = Int32.Parse(formwidths);
        //    string formheights = this.Height.ToString();
        //    int formheight = Int32.Parse(formheights);
        //    int finalx = formwidth / 2 + 76;
        //    int finaly = formheight / 12;
        //    int[] widthHeight = new int[2];
        //    widthHeight[1] = formwidth;
        //    widthHeight[2] = formheight;
        //}

        public void InitTimer()
        {
            timer1 = new System.Windows.Forms.Timer();
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Interval = 8000; // in miliseconds
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            serialPort.Write("1");        //commented out for testing without serial
            //Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Bitmap thermalImage = new Bitmap(256, 256);
            Graphics g = Graphics.FromImage(thermalImage);
            CreateThermalImage(g);
            g.Dispose();
            thermalImage.Save("thermalImage.png");

            Bitmap tableOccupancyImage = new Bitmap(formWidth - 600, formHeight - 200);
            Graphics f = Graphics.FromImage(tableOccupancyImage);
            CreateCurrentTableOccupancyImg(f, seatOccupancy);
            f.Dispose();
            pictureBox1.Image = tableOccupancyImage;
            tableOccupancyImage.Save("table.png");

            Bitmap mainUIContainer = new Bitmap(formWidth, formHeight);
            Graphics h = Graphics.FromImage(mainUIContainer);
            DrawMainUILayout(h);
            pictureBox2.Image = mainUIContainer;
        }

        private void CreateThermalImage(Graphics g)
        {
            ColorMap cm = new ColorMap();
            Random rand = new Random();
            int[,] cmap = cm.Jet();
            int x0 = 10, y0 = 10;
            int width = 30, height = 30;   
            PointC[,] pts = new PointC[8, 8];
            int p = 0;
            float minTemp = pixelValues.Min();
            float maxTemp = pixelValues.Max();

            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.Location = new System.Drawing.Point(0, 0);

            //Remap float values to uint8 range 0-255
            for (int i = 0; i < 64; i++){
                pixelValues[i] = RemapFloatToInt(pixelValues[i], minTemp, maxTemp, 0,255);
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

            // Create original pixelated color image using colour mapping in cmap
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
            
           //Create points used for drawing image
            p = 0;
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    pts[i, j] = new PointC(new PointF(x0 + width * j, y0 + height * i), pixelValues[p]);
                    p += 1;
                }
            }

            //Draw new interpolated image using the pixelated colour image create above
            for (int i = 0; i < 7; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    PointF[] pta = new PointF[4]{pts[i,j].pointf, pts[i+1,j].pointf,
                        pts[i+1,j+1].pointf, pts[i,j+1].pointf};
                    float[] cdata = new float[4]{pts[i,j].C,pts[i+1,j].C,
                        pts[i+1,j+1].C,pts[i,j+1].C};
                    Interp(g, pta, cdata, 50);
                }
            }     

        }

        float RemapFloatToInt(float from, float fromMin, float fromMax, int toMin, int toMax)
        {
            var fromAbs = from - fromMin;
            var fromMaxAbs = fromMax - fromMin;

            var normal = fromAbs / fromMaxAbs;

            var toMaxAbs = toMax - toMin;
            var toAbs = toMaxAbs * normal;

            var to = toAbs + toMin;

            return to;
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

            //Create a new colour mapping
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

            //Draw the points 
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

        bool[] DetectBlobs()
        {
            Bitmap im1 = AForge.Imaging.Image.FromFile(@"C:\Users\jorgo\OneDrive\Documents\EGB340\Blobs\Blobs\Blobs\testImage.png");

            //get image dimension
            int width = im1.Width;
            int height = im1.Height;
            int blobCount = 0;
            Bitmap rbmp = new Bitmap(im1);

            bool[] occupancy = new bool[4];

            //red green blue image
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //get pixel value
                    Color p = im1.GetPixel(x, y);

                    //extract ARGB value from p
                    int a = p.A;
                    int r = p.R;
                    //int g = p.G;
                    //int b = p.B;

                    //set red image pixel
                    rbmp.SetPixel(x, y, Color.FromArgb(a, r, 0, 0));

                }
            }

            //threshold & find blobs
            BlobCounter counter = new BlobCounter();
            counter.BackgroundThreshold = Color.FromArgb(210, 255, 255);
            counter.ProcessImage(rbmp);
            Blob[] blobs = counter.GetObjects(rbmp, true);

            //determine output values
            float[] outputX = new float[blobs.Length];
            float[] outputY = new float[blobs.Length];
            for (int i = 0; i < blobs.Length; i++)
            {
                if (blobs[i].Area > 100)
                {
                    // If you want to output the image of each blob with reference to the rest of the image
                    Bitmap output = blobs[i].Image.ToManagedImage();
                    output.Save(@"C:\Users\jorgo\OneDrive\Documents\EGB340\Blobs\Blobs\Blobs\OutputBlobs\" + i.ToString() + ".png");
                    blobCount += 1;
                    outputX[i] = blobs[i].CenterOfGravity.X;
                    outputY[i] = blobs[i].CenterOfGravity.Y;
                    Console.WriteLine("X: " + outputX[i].ToString() + " Y: " + outputX[i].ToString());
                }
            }

            occupancy = NumPeopleAtTable(blobCount, outputX, outputY);
            return occupancy;
        }


        bool[] NumPeopleAtTable(int blobCount, float[] outputX, float[] outputY)
        {
            bool[] tableOccupancy = new bool[4];

            return tableOccupancy;
        }

        void CreateCurrentTableOccupancyImg(Graphics f, bool[] seatOccupied)
        {
            int pictureBoxWidth = pictureBox1.Size.Width;
            int pictureBoxHeight = pictureBox1.Size.Height;
           

            int[] table1CenterLoc = { (pictureBoxWidth / 2), (pictureBoxHeight / 2) };
            DrawTable(f, table1CenterLoc, seatOccupied);
            
            //Draw a border
            f.DrawRectangle(new Pen(Brushes.LightGray, 2), new Rectangle(0, 0, pictureBoxWidth, pictureBoxHeight));
        }

        private void DrawTable(Graphics f, int[] table1CenterLoc, bool[] seatOccupied)
        {
            int tableLeftEdge = table1CenterLoc[0] - 200;
            int tableRightEdge = table1CenterLoc[0] + 200;
            int tableTopEdge = table1CenterLoc[1] - 300;

            // Table dimensions are in order from chair 1 - 4, counterclockwise from top left chair
            int[,] chairLocationsXY = new int[,] { { tableLeftEdge - 100, tableTopEdge + 100 },
                                                 { tableLeftEdge - 100, tableTopEdge + 400 },
                                                 { tableRightEdge, tableTopEdge + 100 },
                                                 { tableRightEdge, tableTopEdge + 400 } };


            SolidBrush table1 = new SolidBrush(Color.Black);
            f.DrawRectangle(new Pen(Brushes.DarkGray, 5), table1CenterLoc[0] - 200, table1CenterLoc[1] - 300, 400, 600);
            table1.Dispose();

            if (seatOccupied[0] == true)
            {
                DrawChair(f, Color.Red, chairLocationsXY[0,0], chairLocationsXY[0,1]);
            }
            else
            {
                DrawChair(f, Color.Green, chairLocationsXY[0, 0], chairLocationsXY[0, 1]);
            }


            if (seatOccupied[1] == true)
            {
                DrawChair(f, Color.Red, chairLocationsXY[1, 0], chairLocationsXY[1, 1]);
            }
            else
            {
                DrawChair(f, Color.Green, chairLocationsXY[1, 0], chairLocationsXY[1, 1]);
            }


            if (seatOccupied[2] == true)
            {
                DrawChair(f, Color.Red, chairLocationsXY[2, 0], chairLocationsXY[2, 1]);
            }
            else
            {
                DrawChair(f, Color.Green, chairLocationsXY[2, 0], chairLocationsXY[2, 1]);
            }


            if (seatOccupied[3] == true)
            {
                DrawChair(f, Color.Red, chairLocationsXY[3, 0], chairLocationsXY[3, 1]);
            }
            else
            {
                DrawChair(f, Color.Green, chairLocationsXY[3, 0], chairLocationsXY[3, 1]);
            }
            
        }

        void DrawChair(Graphics g,Color occpancyColour, int chairLocationX, int chairLocationY)
        {
            SolidBrush chair = new SolidBrush(occpancyColour);
            g.FillRectangle(chair, new Rectangle(chairLocationX, chairLocationY, 100, 100));
            chair.Dispose();
        }

        void DrawMainUILayout(Graphics h)
        {
            h.SmoothingMode = SmoothingMode.AntiAlias;
            h.InterpolationMode = InterpolationMode.HighQualityBicubic;
            h.PixelOffsetMode = PixelOffsetMode.HighQuality;

            RectangleF rectf = new RectangleF(0, 0, formWidth, 70);
            SolidBrush blueBrush = new SolidBrush(Color.DarkCyan);
            h.FillRectangle(blueBrush, rectf);

            h.SmoothingMode = SmoothingMode.AntiAlias;
            h.InterpolationMode = InterpolationMode.HighQualityBicubic;
            h.PixelOffsetMode = PixelOffsetMode.HighQuality;
            h.DrawString("SEATMASTER 3000", new Font("Tahoma", 22), Brushes.Black, 20,15);

            h.Flush();
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
            audioLevelString =  serialPort.ReadLine();

            //pixelValueString = bytesToString(highByte, lowByte);
            pixelValues = bytesToFloat(highByte, lowByte);

            //Have to use invoke as we are calling the method from a non-primary thread
            this.BeginInvoke(new SetTextDeleg(si_DataReceived), new object[] { audioLevelString });
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

        private void si_DataReceived(string data)
        {
            richTextBox1.Text = data.Trim(); // test with larger textbox
            Invalidate();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            timer1.Start();       //Start getting data once requested
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }
    }
}