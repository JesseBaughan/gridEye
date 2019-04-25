using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;

namespace WindowsApplication14
{
    public partial class Form1 : Form
    {
        //the colors we will be using
        static readonly UInt16 [] camColors = {0x480F,
        0x400F,0x400F,0x400F,0x4010,0x3810,0x3810,0x3810,0x3810,0x3010,0x3010,
        0x3010,0x2810,0x2810,0x2810,0x2810,0x2010,0x2010,0x2010,0x1810,0x1810,
        0x1811,0x1811,0x1011,0x1011,0x1011,0x0811,0x0811,0x0811,0x0011,0x0011,
        0x0011,0x0011,0x0011,0x0031,0x0031,0x0051,0x0072,0x0072,0x0092,0x00B2,
        0x00B2,0x00D2,0x00F2,0x00F2,0x0112,0x0132,0x0152,0x0152,0x0172,0x0192,
        0x0192,0x01B2,0x01D2,0x01F3,0x01F3,0x0213,0x0233,0x0253,0x0253,0x0273,
        0x0293,0x02B3,0x02D3,0x02D3,0x02F3,0x0313,0x0333,0x0333,0x0353,0x0373,
        0x0394,0x03B4,0x03D4,0x03D4,0x03F4,0x0414,0x0434,0x0454,0x0474,0x0474,
        0x0494,0x04B4,0x04D4,0x04F4,0x0514,0x0534,0x0534,0x0554,0x0554,0x0574,
        0x0574,0x0573,0x0573,0x0573,0x0572,0x0572,0x0572,0x0571,0x0591,0x0591,
        0x0590,0x0590,0x058F,0x058F,0x058F,0x058E,0x05AE,0x05AE,0x05AD,0x05AD,
        0x05AD,0x05AC,0x05AC,0x05AB,0x05CB,0x05CB,0x05CA,0x05CA,0x05CA,0x05C9,
        0x05C9,0x05C8,0x05E8,0x05E8,0x05E7,0x05E7,0x05E6,0x05E6,0x05E6,0x05E5,
        0x05E5,0x0604,0x0604,0x0604,0x0603,0x0603,0x0602,0x0602,0x0601,0x0621,
        0x0621,0x0620,0x0620,0x0620,0x0620,0x0E20,0x0E20,0x0E40,0x1640,0x1640,
        0x1E40,0x1E40,0x2640,0x2640,0x2E40,0x2E60,0x3660,0x3660,0x3E60,0x3E60,
        0x3E60,0x4660,0x4660,0x4E60,0x4E80,0x5680,0x5680,0x5E80,0x5E80,0x6680,
        0x6680,0x6E80,0x6EA0,0x76A0,0x76A0,0x7EA0,0x7EA0,0x86A0,0x86A0,0x8EA0,
        0x8EC0,0x96C0,0x96C0,0x9EC0,0x9EC0,0xA6C0,0xAEC0,0xAEC0,0xB6E0,0xB6E0,
        0xBEE0,0xBEE0,0xC6E0,0xC6E0,0xCEE0,0xCEE0,0xD6E0,0xD700,0xDF00,0xDEE0,
        0xDEC0,0xDEA0,0xDE80,0xDE80,0xE660,0xE640,0xE620,0xE600,0xE5E0,0xE5C0,
        0xE5A0,0xE580,0xE560,0xE540,0xE520,0xE500,0xE4E0,0xE4C0,0xE4A0,0xE480,
        0xE460,0xEC40,0xEC20,0xEC00,0xEBE0,0xEBC0,0xEBA0,0xEB80,0xEB60,0xEB40,
        0xEB20,0xEB00,0xEAE0,0xEAC0,0xEAA0,0xEA80,0xEA60,0xEA40,0xF220,0xF200,
        0xF1E0,0xF1C0,0xF1A0,0xF180,0xF160,0xF140,0xF100,0xF0E0,0xF0C0,0xF0A0,
        0xF080,0xF060,0xF040,0xF020,0xF800,};

        int minTemp = 22, maxTemp = 34;
        int gridEyePixelArraySize = 64;
        float delayTime;
        float[] pixels = new float[64];
        int displayPixelWidth, displayPixelHeight;

        SerialPort serialPort;
        Thread dataInThread; //create a thread for continuous data capture

        // delegate is used to write to a UI control from a non-UI thread
        private delegate void SetTextDeleg(string text);

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // all of the options for a serial device
            // can be sent through the constructor of the SerialPort class
            // PortName = "COM1", Baud Rate = 19200, Parity = None, 
            // Data Bits = 8, Stop Bits = One, Handshake = None
            serialPort = new SerialPort("COM3", 115200, Parity.None, 8, StopBits.One);
            serialPort.Handshake = Handshake.None;
            serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
            serialPort.ReadTimeout = 500;
            serialPort.WriteTimeout = 500;
            serialPort.Open();

            //dataInThread = new Thread(DoThisAllTheTime);
           // dataInThread.Start();
        }

        public void DoThisAllTheTime()
        {
            try
            {
                while (true)
                {
                    //you need to use Invoke because the new thread can't access the UI elements directly
                    MethodInvoker mi = delegate ()
                    {
                        this.Text = DateTime.Now.ToString();
                        serialPort.Write("2");
                        //Thread.Sleep(5000);
                    };
                    this.Invoke(mi);
                    Thread.Sleep(1000);
                }
            }
            catch (ThreadAbortException abortException)
            {
                Console.WriteLine((string)abortException.ExceptionState);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Thread.Sleep(2000);
            //dataInThread.Abort();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            // Makes sure serial port is open before trying to write
            try
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                }

                serialPort.Write("1");
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
            //dataInThread.Abort();
        }

        void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] highByte = new byte[64];
            byte[] lowByte = new byte[64];
            var pixel = new float[64];
            UInt16 pixelValueInt = 0;
            int bytesSent = 64;

            string data = "";
            string pixelDat = "";

            Thread.Sleep(500);

            //Read high and low bytes from serial port
            serialPort.Read(highByte, 0, 64);
            serialPort.Read(lowByte, 0, 64);

            //Convert from bytes back to float values
            for (int i = 1; i <= bytesSent; i++)
            {
                pixelValueInt = (UInt16)(highByte[i-1] * 256 + lowByte[i-1]);
                pixel[i-1] = (float)(pixelValueInt / 100.0);

                pixelDat = pixel[i-1].ToString();
                data = data + " " + pixelDat;
                if (i % 8 == 0) { data = data + "\n"; }
            }

            //Have to use invoke as we are calling the method from a non-primary thread
            this.BeginInvoke(new SetTextDeleg(si_DataReceived), new object[] { data });
        }

        private void si_DataReceived(string data)
        {  
            richTextBox1.Text = data.Trim(); // test with larger textbox
        }
    }
}