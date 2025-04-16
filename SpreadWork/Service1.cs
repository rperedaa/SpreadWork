using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace SpreadWork
{
    public partial class Service1 : ServiceBase
    {
        System.Timers.Timer WatchDog = new System.Timers.Timer();
        int Interval = 10000;

        public Service1()
        {
            InitializeComponent();
            this.ServiceName = "SpreadWorkService";
        }

        protected override void OnStart(string[] args)
        {
            #if DEBUG
            System.Diagnostics.Debugger.Launch();
            #endif
            // Service Code
            WriteLog("Servicio SpreadWorkService arrancado");
            // Ejecución períodica del servicio
            WatchDog.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            WatchDog.Interval = Interval;
            WatchDog.Enabled = true;
        }

        private void OnElapsedTime(object sender, ElapsedEventArgs e)
        {
            WatchDog.Enabled = false;
            // Escribe log
            WriteLog("{0} ms elapsed.");
            // Captura imagen

            /*
            Rectangle bounds = Screen.GetBounds(Point.Empty);
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }
                bitmap.Save("test.jpg", ImageFormat.Jpeg);
            }
            */
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            var rect = screen.Bounds;
            var size = rect.Size;

            Bitmap bmpScreenshot = new Bitmap(size.Width, size.Height);
            Graphics g = Graphics.FromImage(bmpScreenshot);
            g.CopyFromScreen(0, 0, 0, 0, size);
            bmpScreenshot.Save("test.jpg", ImageFormat.Jpeg);


            // Inicializa Timer
            WatchDog.Enabled = true;
        }

        private void WriteLog(string logMessage, bool addTimeStamp = true)
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var filePath = String.Format("{0}\\{1}_{2}.txt",
                path,
                ServiceName,
                DateTime.Now.ToString("yyyyMMdd", CultureInfo.CurrentCulture)
                );

            if (addTimeStamp)
                logMessage = String.Format("[{0}] - {1}",
                    DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture),
                    logMessage);

            File.AppendAllText(filePath, logMessage);
        }
    

        protected override void OnStop()
        {
            Timer.Stop();
            WriteLog("Servicio SpreadWorkService detenido");
        }
    }
}
