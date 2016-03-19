using Gadgeteer.Modules.GHIElectronics;
using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using GHI.Processor;
using System.IO;

namespace App_IFU2
{
    
    public partial class Program
    {
        #region Flashing Firmware / App
        public const int BLOCK_SIZE = 65536;

        public static void FlashFirmware()
        {
            // Reserve the memory needed to buffer the update.
            // A lot of RAM is needed so it is recommended to do this at the program start.
            InFieldUpdate.Initialize(InFieldUpdate.Types.Application);

            // Start loading the new firmware on the RAM reserved in last step.
            // Nothing is written to FLASH In this stage. Power loss and failures are okay
            // Simply abort this stage any way you like!
            // Files can come from Storage, from network, from serial bus and any Other way.
            LoadFile("\\SD\\IFU\\aplikasi1.hex", InFieldUpdate.Types.Application);

            //LoadFile("\\SD\\Config.hex", InFieldUpdate.Types.Configuration);
            //LoadFile("\\SD\\Firmware.hex", InFieldUpdate.Types.Firmware);
            //LoadFile("\\SD\\Firmware2.hex", InFieldUpdate.Types.Firmware); //Only if your device has two firmware files.

            // This method will copy The new firmware from RAM to FLASH.
            // This function will not return But will reset the system when done.
            // Power loss during Before this function resets the system quill result in a corrupted firmware.
            // A manual update will be needed if this method failed, due to power loss for example.
            InFieldUpdate.FlashAndReset();
        }

        public static void LoadFile(string filename, InFieldUpdate.Types type)
        {
            using (var stream = new FileStream(filename, FileMode.Open))
            {
                var data = new byte[BLOCK_SIZE];

                for (int i = 0; i < stream.Length / BLOCK_SIZE; i++)
                {
                    stream.Read(data, 0, BLOCK_SIZE);
                    InFieldUpdate.Load(type, data, BLOCK_SIZE);
                }

                stream.Read(data, 0, (int)stream.Length % BLOCK_SIZE);
                InFieldUpdate.Load(type, data, (int)stream.Length % BLOCK_SIZE);
            }
        }
        // This method is run when the mainboard is powered up or reset.   
        #endregion

        Window mainWindow;
        public const int MaxStars = 50;
        public const int MaxHistory = 3;
        public const double PI2 = 6.283185307179586476925286766559;
        public static Random Rnd = new Random();
        public void StartStarsShow()
        {
            mainWindow = displayTE35.WPFWindow;
            Mainboard.LDR0.OnInterrupt += LDR0_OnInterrupt;
            //display
            Bitmap bmp = new Bitmap(320, 240);
            Image img = new Image(bmp);
            mainWindow.Child = img;
            Star[] stars = new Star[MaxStars];
            var CurrentFont = Resources.GetFont(Resources.FontResources.NinaB);

            for (int i = 0; i < MaxStars; i++)
            {
                stars[i] = new Star();
            }

            while (true)
            {
                for (int i = 0; i < MaxStars; i++)
                {
                    var star = stars[i];

                    if (star.X < 0 || star.X > 320 || star.Y < 0 || star.Y > 240)
                    {
                        for (int j = 0; j <= MaxHistory; j++)
                        {
                            bmp.SetPixel(star.X - j * star.Dx, star.Y - j * star.Dy, Color.Black);
                        }

                        star.Initialize();
                    }
                    else
                    {
                        bmp.SetPixel(star.X, star.Y, Color.White);
                        star.X += star.Dx;
                        star.Y += star.Dy;

                        if (star.History < MaxHistory)
                        {
                            star.History++;
                        }
                        else
                        {
                            bmp.SetPixel(star.X - (MaxHistory + 1) * star.Dx, star.Y - (MaxHistory + 1) * star.Dy, Color.Black);
                        }
                    }
                }

                bmp.DrawText("Hello, I'm the second firmware...", CurrentFont, GT.Color.Yellow, 60, 120);
                bmp.DrawText("Press LDR 0 to flash firmware 1..", CurrentFont, GT.Color.Orange, 60, 140);

                img.Invalidate();
                //displayTE35.SimpleGraphics.DisplayImage(bmp, 0, 0);
                bmp.Flush();
            }
        }

        private void LDR0_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            //release handler
            Mainboard.LDR0.OnInterrupt -= LDR0_OnInterrupt;
            //flashback to firmware 1
            FlashFirmware();
        }

        class Star
        {
            public int X;
            public int Y;
            public int Dx;
            public int Dy;
            public int History;

            public Star()
            {
                Initialize();
            }

            public void Initialize()
            {
                do
                {

                    var angle = PI2 * Rnd.NextDouble();
                    var speed = Rnd.Next(5) + 1;

                    Dx = (int)System.Math.Round(System.Math.Sin(angle) * speed);
                    Dy = (int)System.Math.Round(System.Math.Cos(angle) * speed);
                } while (Dx == 0 || Dy == 0);

                X = 8 * Dx + 160;
                Y = 8 * Dy + 120;
                History = 0;
            }
        }
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            Debug.Print("Program Started");
            var th1 = new Thread(new ThreadStart(StartStarsShow));
            th1.Start();

        }

    }
}
