using System.IO;

namespace WpfApp.Settings
{
    public static class AppFilePaths
    {
        private const string Resources = "resources";
        private static string Images { get; } = Path.Combine(Resources, "images");
        public static string PersonImages { get; } = Path.Combine(Images, "people");
        public static string ChildImages { get; } = PersonImages;
        public static string ParentImages { get; } = PersonImages;
        public static readonly string NoImage = Resources + Path.DirectorySeparatorChar + "no_picture.jpg";

        public static void CreateAllDirectories()
        {
            Directory.CreateDirectory(PersonImages);
            App.Logger.Trace("Directories created");
        }
    }
}