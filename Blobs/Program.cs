using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using AForge;
using AForge.Imaging;
using AForge.Imaging.ColorReduction;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;

namespace Blobs
{   
    class Program
    {
        static void Main(string[] args)
        {   /*
            //test array (replace with real data)
            double[,] temps = new double[8, 8] { { 20, 20, 20, 20, 20, 20, 20, 20},
                                                 { 20, 25, 26, 24, 20, 20, 20, 20},
                                                 { 20, 27, 28, 26, 20, 20, 20, 20},
                                                 { 20, 25, 27, 25, 20, 20, 20, 20},
                                                 { 20, 20, 20, 20, 21, 23, 21, 20},
                                                 { 20, 20, 20, 20, 21, 34, 22, 20},
                                                 { 19, 20, 20, 20, 21, 24, 21, 20},
                                                 { 18, 19, 20, 20, 20, 20, 20, 20} };

            //find the average temp
            double aveSum = 0;
            for (int x = 0; x < temps.GetLength(0); x++)
            {
                for (int y = 0; y < temps.GetLength(1); y++)
                {
                    aveSum += temps[x, y];
                }
            }
            double ave = aveSum / temps.Length;

            //turn binary array into image
            Bitmap im1 = new Bitmap(temps.GetLength(0), temps.GetLength(1));

            //threshhold by average temp (accept temps that are greater than the average + 2 degrees)
            for (int x = 0; x < temps.GetLength(0); x++)
            {
                for (int y = 0; y < temps.GetLength(1); y++)
                {
                    if (temps[x,y] > ave+2)
                    {
                        im1.SetPixel(x, y, Color.FromArgb(Int32.MaxValue));
                    }
                    else
                    {
                        im1.SetPixel(x, y, Color.FromArgb(Int32.MinValue));
                    }
                }
            }*/
            
            //taking image from file instead
            Bitmap im1 = AForge.Imaging.Image.FromFile(@"C:\Users\jorgo\OneDrive\Documents\EGB340\Blobs\Blobs\Blobs\testImage.png");

            //get image dimension
            int width = im1.Width;
            int height = im1.Height;

            //3 bitmap for red green blue image
            Bitmap rbmp = new Bitmap(im1);
            //Bitmap gbmp = new Bitmap(im1);
            //Bitmap bbmp = new Bitmap(im1);

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

                    //set green image pixel
                    //gbmp.SetPixel(x, y, Color.FromArgb(a, 0, g, 0));

                    //set blue image pixel
                    //bbmp.SetPixel(x, y, Color.FromArgb(a, 0, 0, b));

                }
            }

            //threshold & find blobs
            BlobCounter counter = new BlobCounter();
            counter.BackgroundThreshold = Color.FromArgb(210, 255, 255);
            counter.ProcessImage(rbmp);
            Blob[] blobs = counter.GetObjects(rbmp, true);

            //determine output values
            int blobcount = 0;
            float[] outputX = new float[blobs.Length];
            float[] outputY = new float[blobs.Length];
            for (int i = 0; i < blobs.Length; i++)
            {
                if (blobs[i].Area > 100)
                {
                    // If you want to output the image of each blob with reference to the rest of the image
                    Bitmap output = blobs[i].Image.ToManagedImage();
                    output.Save(@"C:\Users\jorgo\OneDrive\Documents\EGB340\Blobs\Blobs\Blobs\OutputBlobs\" + i.ToString() + ".png");
                    blobcount += 1;
                    outputX[i] = blobs[i].CenterOfGravity.X;
                    outputY[i] = blobs[i].CenterOfGravity.Y;
                    Console.WriteLine("X: " + outputX[i].ToString() + " Y: " + outputY[i].ToString());
                }
            }
            Console.WriteLine("Number of Blobs: " + blobcount.ToString());
            Console.ReadKey();
        }
    }

   
}
