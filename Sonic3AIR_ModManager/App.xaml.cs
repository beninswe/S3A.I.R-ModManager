﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Sonic3AIR_ModManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 

    public enum Skin { Dark, Light }
    public partial class App : Application
    {
        public static App Instance;

        public static Skin Skin { get; set; } = Skin.Dark;

        public static bool SkinChanged { get; set; } = false;


        public App()
        {
            if (Sonic3AIR_ModManager.Properties.Settings.Default.UseDarkTheme == true) ChangeSkin(Skin.Dark);
            else ChangeSkin(Skin.Light);

            Instance = this;
        }

        public void RunAutoBoot()
        {
            this.InitializeComponent();
            var auto = new AutoBootDialogV2();
            if (auto.ShowDialog() == true)
            {
                if (Program.AutoBootCanceled == false) this.Run(new ModManager(true));
                else this.Run(new ModManager(false));
            }

        }

        public void GBAPI(string Arguments)
        {
            this.InitializeComponent();
            this.Run(new ModManager(Arguments));
        }


        public void DefaultStart()
        {
            this.InitializeComponent();
            this.Run(new ModManager());
        }


        public static void ChangeSkin(Skin newSkin)
        {
            Skin = newSkin;

            foreach (ResourceDictionary dict in Sonic3AIR_ModManager.App.Current.Resources.MergedDictionaries)
            {

                if (dict is SkinResourceDictionary skinDict)
                    skinDict.UpdateSource();
                else
                    dict.Source = dict.Source;
            }
        }
    }
}
