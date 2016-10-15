using System.Configuration;
using System.Text.RegularExpressions;
// ReSharper disable UnusedMember.Local

namespace DAL
{
    public static class DatabaseConfig
    {
        public const string ConnectionStringName = "KindergartenContext";

        private static string ReplaceConnectionStringPath(string connectionString, string newPath)
        {
            var pathGroup = new Regex(@"attachdbfilename\W*=(\W*.*?)\;")
                .Match(connectionString)
                .Groups[1];

            if (!pathGroup.Success) throw new ConfigurationErrorsException("Invalid connection string: attachdbfilename=%path%;. See app.config");

            var newConnectionString = connectionString.Substring(0, pathGroup.Index) +
                                      newPath +
                                      connectionString.Substring(pathGroup.Index + pathGroup.Length);
            return newConnectionString;
        }

        public static bool EnsureConnectionString(string path)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConnectionStringsSection section = (ConnectionStringsSection)config.GetSection("connectionStrings");

            var s = section.ConnectionStrings[ConnectionStringName];
            var correct = GetConnectionStringSettings(path);

            if (s == null)
            {
                // add
                section.ConnectionStrings.Add(correct);
            }
            else if (correct.ConnectionString != s.ConnectionString || correct.ProviderName != s.ProviderName)
            {
                // change
                s.ConnectionString = correct.ConnectionString;
                s.ProviderName = correct.ProviderName;
            }
            else
            {
                // nothing
                return false;
            }
            config.Save();
            return true;
        }

        private static ConnectionStringSettings GetConnectionStringSettings(string path)
        {
            string s = @"data source=(LocalDB)\MSSQLLocalDB;attachdbfilename=" + path +
                       ";integrated security=True;MultipleActiveResultSets=True;App=EntityFramework";

            return new ConnectionStringSettings(ConnectionStringName, s)
            {
                ProviderName = "System.Data.SqlClient"
            };
        }
    }
}