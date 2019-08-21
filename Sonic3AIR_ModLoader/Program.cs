﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sonic3AIR_ModLoader
{
    static class Program
    {
        public static bool AutoBootCanceled = false;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (Properties.Settings.Default.AutoLaunch)
            {
                Application.Run(new AutoBootDialog());
                if (AutoBootCanceled == false) Application.Run(new ModManager(true));
                else Application.Run(new ModManager(false));
            }
            else
            {
                Application.Run(new ModManager());
            }


        }
    }
}
