using System;
using System.IO;
using DAL.Model;

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
        public static readonly string Documents = Path.Combine(Resources, "documents");
        public static readonly string DocTemplates = Path.Combine(Resources, "templates");

        public static void CreateAllDirectories()
        {
            Directory.CreateDirectory(PersonImages);
            App.Logger.Trace("Directories created");
        }

        public static string GetDocumentsDirecoryPathForChild(Child child, int year)
        {
            return Path.Combine(Documents, year.ToString(), child.Id.ToString());
        }

        public static string GetAddedChildDocumentFileName(Child addedChild)
        {
            return addedChild.Id + "_added.docx";
        }

        public static string GetAddedChildDocumentTemplatePath()
        {
            return Path.Combine(DocTemplates, "child_added.docx");
        }
    }
}