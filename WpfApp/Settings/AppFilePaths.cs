using System.IO;

namespace WpfApp.Settings
{
    public static class AppFilePaths
    {
        private static string Resources { get; } = "resources";
        private static string Images { get; } = Path.Combine(Resources, "images");
        public static string PersonImages { get; } = Path.Combine(Images, "people");
        public static string ChildImages { get; } = PersonImages;
        public static string ParentImages { get; } = PersonImages;

        public static void CreateAllDirectories()
        {
            Directory.CreateDirectory(PersonImages);
            App.Logger.Trace("Directories created");
        }
    }
}