﻿#if DEBUG
#else
using System;
using System.Windows;
#endif
using WpfApp.Settings;

namespace WpfApp
{
    public class EntryPoint
    {
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public static void Main(string[] args)
        {
#if DEBUG
            Database.Load();
            EFlogger.EntityFramework6.EFloggerFor6.Initialize();
            AppFilePaths.CreateAllDirectories();
            App.Main();
#else
            try
            {
                Database.Load();
                AppFilePaths.CreateAllDirectories();
                App.Main();
            }
            catch (Exception e)
            {
                App.Logger.Fatal(e, "Exception");
                MessageBox.Show("В программе произошла ошибка, пожалуйста, свяжитесь с разработчиками...", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
#endif
        }
    }
}