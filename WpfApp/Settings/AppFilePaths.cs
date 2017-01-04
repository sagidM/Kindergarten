using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using DAL.Model;
using Newtonsoft.Json;

namespace WpfApp.Settings
{
    public static class AppFilePaths
    {
        private const string Resources = "resources";
        public static readonly string NoImage = Resources + Path.DirectorySeparatorChar + "no_picture.jpg";
        private const string DefaultResourceStructureFile = "resources/ru_default_resource_structure.json";
        private static readonly string ResourceStructurePath = Resources + Path.DirectorySeparatorChar + "resource_structure.json";
        public static IDocumentSettings MyDocumentSettings { get; }
        public static string PersonImages { get; }
        public static string ChildImages { get; }
        public static string ParentImages { get; }
        public static readonly string DocTemplates = Resources + Path.DirectorySeparatorChar + "templates";
        
        public static string GetDocumentsDirectoryPathForChild(Child child, int year)
        {
            var c = Path.DirectorySeparatorChar;
            return Resources + c + MyDocumentSettings.Documents + c + year + c + child.Id;
        }

        /// <summary>Returns relative path of template</summary>
        private static string GetDocumentName(string document)
        {
            return Resources + Path.DirectorySeparatorChar +
                MyDocumentSettings.Templates + Path.DirectorySeparatorChar +
                MyDocumentSettings.FilenameOfDocumentsAndTemplates[document][0];
        }
        /// <summary>Returns file name of child's document</summary>
        private static string GetDocumentName(string document, int childId)
        {
            return string.Format(MyDocumentSettings.FilenameOfDocumentsAndTemplates[document][1], childId);
        }

        public static string GetAgreementFileName(Child child) => GetDocumentName("agreement", child.Id);
        public static string GetAgreementTemplatePath() => GetDocumentName("agreement");
        
        public static string GetPortfolioFileName(Child child) => GetDocumentName("portfolio", child.Id);
        public static string GetPortfolioTemplatePath() => GetDocumentName("portfolio");

        public static string GetTakingChildFileName(Child child) => GetDocumentName("taking_child", child.Id);

        public static string GetTakingChildTemplatePath() => GetDocumentName("taking_child");

        public static string GetOrderOfAdmissionFileName(Child child) => GetDocumentName("order_of_admission", child.Id);
        public static string GetOrderOfAdmissionFileName(Child child, DateTime enterDate) =>
            string.Format(MyDocumentSettings.FilenameOfDocumentsAndTemplates["order_of_admission"][1], child.Id, "_" + enterDate.ToString(OtherSettings.DateFormat));
        public static string GetOrderOfAdmissionTemplatePath() => GetDocumentName("order_of_admission");

        public static string GetNoticeFileName() => MyDocumentSettings.FilenameOfDocumentsAndTemplates["notices"][1];
        public static string GetNoticeTemplatePath() => GetDocumentName("notices");

        public static string GetMonthlyReceiptFileName(Child child) => GetDocumentName("monthly_receipt", child.Id);
        public static string GetMonthlyReceiptTemplatePath() => GetDocumentName("monthly_receipt");

        public static string GetAnnualReceiptFileName(Child child) => GetDocumentName("annual_receipt", child.Id);
        public static string GetAnnualReceiptTemplatePath() => GetDocumentName("annual_receipt");

        public static string GetAddingToArchiveFileName(Child child) => GetDocumentName("adding_to_archive", child.Id);
        public static string GetAddingToArchiveFileName(Child child, DateTime expultionDate) =>
            string.Format(MyDocumentSettings.FilenameOfDocumentsAndTemplates["adding_to_archive"][1], child.Id, "_" + expultionDate.ToString(OtherSettings.DateFormat));
        public static string GetAddingToArchiveTemplatePath() => GetDocumentName("adding_to_archive");

        public static string GetGroupTransferFileName(Child child) => GetDocumentName("transfer_group", child.Id);
        public static string GetGroupTransferFileName(Child child, DateTime expultionDate, string groupName, string groupType) =>
            string.Format(MyDocumentSettings.FilenameOfDocumentsAndTemplates["transfer_group"][1],
                child.Id,
                expultionDate.ToString(OtherSettings.DateFormat),
                groupName,
                groupType);
        public static string GetGroupTransferTemplatePath() => GetDocumentName("transfer_group");

        public static string GetGroupTypeChangedFileName() =>
            MyDocumentSettings.FilenameOfDocumentsAndTemplates["group_type_changed"][1];
        public static string GetGroupTypeChangedTemplatePath() => GetDocumentName("group_type_changed");


        static AppFilePaths()
        {
            DocumentSettings docSettings = null;
            if (File.Exists(ResourceStructurePath))
            {
                var json = File.ReadAllText(ResourceStructurePath, OtherSettings.Encoding);
                try
                {
                    docSettings = JsonConvert.DeserializeObject<DocumentSettings>(json);
                }
                catch (Exception e)
                {
                    App.Logger.Warn(e, $"Cannot deserialize \"{ResourceStructurePath}\"");
                }
            }
            if (docSettings == null)
            {
                var defRes = new Uri(DefaultResourceStructureFile, UriKind.Relative);
                string defJson;
                // ReSharper disable once PossibleNullReferenceException
                using (var stream = new StreamReader(Application.GetResourceStream(defRes).Stream))
                    defJson = stream.ReadToEnd();
                App.Logger.Info("Default resource structure file initialized + \"" + DefaultResourceStructureFile + "\"");

                docSettings = JsonConvert.DeserializeObject<DocumentSettings>(defJson);

                var settings = JsonConvert.SerializeObject(docSettings, Formatting.Indented);
                File.WriteAllText(ResourceStructurePath, settings, OtherSettings.Encoding);
            }
            MyDocumentSettings = docSettings;
            ParentImages = ChildImages = PersonImages = Path.Combine(Resources, MyDocumentSettings.Images, "people");
        }

        public interface IDocumentSettings
        {
            string Documents { get; set; }
            string Images { get; set; }
            string Templates { get; set; }
            IDictionary<string, string[]> FilenameOfDocumentsAndTemplates { get; set; }
        }
        public class DocumentSettings : IDocumentSettings
        {
            public string Images { get; set; }
            public string Documents { get; set; }
            public string Templates { get; set; }
            public IDictionary<string, string[]> FilenameOfDocumentsAndTemplates { get; set; }
        }
    }
}