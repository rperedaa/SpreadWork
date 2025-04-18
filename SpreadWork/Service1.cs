using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.ServiceProcess;
using System.Timers;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading;
using System.Linq;

namespace SpreadWork
{
    public partial class Service1 : ServiceBase
    {
        System.Timers.Timer WatchDog = new System.Timers.Timer();
        int Interval = 10000; // este valor no es necesario cambiarlo
        private NameValueCollection cfgGlobal;
        string RemotePath = "";
        string LocalPath = "";
        public Service1()
        {
            InitializeComponent();
            this.ServiceName = "SpreadWorkService";
            // Cargar Configuración
            LoadProgramConfig();
            SetProgramConfig();
        }

        protected override void OnStart(string[] args)
        {
            #if DEBUG
            System.Diagnostics.Debugger.Launch();
            #endif
            // Log
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
            WriteLog(Interval + " ms elapsed.");
            // Comprobar si existe programa en Archivos de programa .x86
            if (!IsProgramInstalled("SpreadWork"))
            {
                WriteLog("Programa SpreadWork no instalado.");
            }
            else
            {
                // Comprobar si programa arrancado 
                if (!IsProgramRunning("SpreadWorkDesktop"))
                {
                    WriteLog("Programa SpreadWorkDesktop no esta en ejecución.");
                    // Arrancar programa
                    if (File.Exists("C:\\Program Files (x86)\\SpreadWork\\SpreadWorkDesktop.exe"))
                    {
                        WriteLog("Intentando arrancar SpreadWorkDesktop.");
                        try
                        {
                            System.Diagnostics.Process.Start("C:\\Program Files (x86)\\SpreadWork\\SpreadWorkDesktop.exe");
                        }
                        catch (Exception ex) {
                            WriteLog("No puedo arrancar el programa C:\\Program Files (x86)\\SpreadWork\\SpreadWorkDesktop.exe : " + ex.Message);
                        }
                    }
                    else {
                        WriteLog("El programa  no está en C:\\Program Files (x86)\\SpreadWork\\SpreadWorkDesktop.exe");
                    }

                }
            }

            // Inicializa Timer
            WatchDog.Enabled = true;
        }

        private void WriteLog(string logMessage, bool addTimeStamp = true)
        {

            try
            {
                //var path = AppDomain.CurrentDomain.BaseDirectory;
                var path = LocalPath;
                if (!Directory.Exists(path))
                {
                    DirectoryInfo di = Directory.CreateDirectory(path);
                    di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
                }

                var filePath = String.Format("{0}\\{1}_{2}.txt",
                    path,
                    System.AppDomain.CurrentDomain.FriendlyName,
                    DateTime.Now.ToString("yyyyMMdd", CultureInfo.CurrentCulture)
                    );

                if (addTimeStamp)
                    logMessage = String.Format("[{0}] - {1}",
                        DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture),
                        logMessage);


                File.AppendAllText(filePath, logMessage);
            }
            catch (Exception e)
            {
                // do nothing
                WriteLog("Exception writing log:" + e.ToString());
            }
        }

        #region Search Program
        public static bool IsProgramInstalled(string programName)
        {
            try
            {
                RegistryKey uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (uninstallKey != null)
                {
                    foreach (string subkeyName in uninstallKey.GetSubKeyNames())
                    {
                        RegistryKey subkey = uninstallKey.OpenSubKey(subkeyName);
                        if (subkey != null)
                        {
                            string displayName = (string)subkey.GetValue("DisplayName");
                            if (displayName == programName)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        
        public static bool IsProgramRunning(string programName)
        {
           Process[] procesos = Process.GetProcessesByName(programName);
           return procesos.Length > 0;
        }

            #endregion

            #region Config
            private void LoadProgramConfig()
        {
            // Leer todos los parámetros del fichero de configuración
            ConfigurationManager.RefreshSection("appSettings");
            cfgGlobal = ConfigurationManager.AppSettings;
        }

        private void SetProgramConfig()
        {
            //RemoteServer
            RemotePath = cfgGlobal.Get("RemotePath");
            LocalPath = cfgGlobal.Get("LocalPath");
        }

        #endregion



        protected override void OnStop()
        {
            WatchDog.Stop();
            WriteLog("Service SpreadWorkService stopped");
            // Matar programa 

            foreach (Process p in System.Diagnostics.Process.GetProcessesByName("SpreadWorkDesktop"))
            {
                try
                {
                    p.Kill();
                    p.WaitForExit();
                }
                catch (Exception e) {
                    WriteLog("I can't stop process SpreadWorkDesktop " + e.Message);
                }
            }



        }
    }
}
