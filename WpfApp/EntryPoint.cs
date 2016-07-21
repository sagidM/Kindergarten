using System;
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
            EFlogger.EntityFramework6.EFloggerFor6.Initialize();
            AppFilePaths.CreateAllDirectories();
            Database.Load();
            App.Main();
        }
    }
}