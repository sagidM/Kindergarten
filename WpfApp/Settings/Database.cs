using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using DAL;

namespace WpfApp.Settings
{
    public class Database
    {
        private const string DatabaseName = "KindergartenDb.mdf";

        // Can restart the program
        public static void Load()
        {
            // TODO: load from arguments
            DatabaseConfig.EnsureConnectionString(Path.GetFullPath(DatabaseName));
            var cs = ConfigurationManager.ConnectionStrings[DatabaseConfig.ConnectionStringName];
            if (cs == null)
            {
                App.Logger.Info("Restart program. The reason is app.config");
                Console.WriteLine("Restarting program...");

                var args = Environment.GetCommandLineArgs();
                Process.Start(args[0], string.Join(" ", args, 1, args.Length-1));
                Process.GetCurrentProcess().Kill();
            }
            App.Logger.Trace("Database file is loaded (" + DatabaseName + ")");
        }
    }
}