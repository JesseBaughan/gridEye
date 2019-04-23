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
            Thread.Sleep(500);
            string data = serialPort.ReadLine();
            this.BeginInvoke(new SetTextDeleg(si_DataReceived), new object[] { data });
        }

        private void si_DataReceived(string data)
        {
            textBox1.Text = data.Trim();
            richTextBox1.Text = data.Trim(); // test with larger textbox
        }
    }
}