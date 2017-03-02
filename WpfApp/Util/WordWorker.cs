using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Office.Interop.Word;
using Application = Microsoft.Office.Interop.Word.Application;

namespace WpfApp.Util
{
    public class WordWorker
    {
        public static void Replace<TKey, TValue>(string templateName, string destination, IDictionary<TKey, TValue> data, string picturePath = null)
        {
            if (!File.Exists(templateName))
            {
                ShowFileNotFound(templateName);
                return;
            }
            var app = new Application();
            try
            {
                var doc = OpenDocument(app, templateName);
                if (doc == null) return;
                try
                {
                    var s = app.Selection;
                    s.Find.ClearFormatting();
                    s.Find.Replacement.ClearFormatting();

                    foreach (var pair in data)
                    {
                        s.Find.Execute(FindText: pair.Key, ReplaceWith: pair.Value, MatchCase: false, Replace: WdReplace.wdReplaceAll);
                    }
                    if (picturePath != null)
                        ReplaceByPicture(s, picturePath, "&img");
                    DocumentSaveAs(doc, destination);
                }
                finally
                {
                    CloseDocuments(app.Documents);
                }
            }
            finally
            {
                app.Quit();
                System.Runtime.InteropServices.Marshal.ReleaseComObject(app);
            }
        }

        public static void InsertTable<TKey, TValue>(string templateName, string destination, IDictionary<TKey, TValue> header, IDictionary<string, TValue>[] body)
        {
            if (!File.Exists(templateName))
            {
                ShowFileNotFound(templateName);
                return;
            }
            var app = new Application();
            try
            {
                var doc = OpenDocument(app, templateName);
                if (doc == null) return;
                try
                {
                    var s = app.Selection;
                    s.Find.ClearFormatting();
                    s.Find.Replacement.ClearFormatting();

                    var tableCount = doc.Tables.Count;
                    for (int tableIndex = 1; tableIndex <= tableCount; tableIndex++)
                    {
                        var table = doc.Tables[tableIndex];
                        if (table.Rows.Count != 1) continue;
                        var headerCells = table
                            .Rows[1]
                            .Cells
                            .Cast<Cell>()
                            .Select(c => CellRegex.Replace(c.Range.Text, ""))
                            .ToArray();

                        foreach (var line in body)
                        {
                            var row = table.Rows.Add();
                            for (int cellIndex = 0; cellIndex < headerCells.Length; cellIndex++)
                            {
                                TValue value;
                                if (line.TryGetValue(headerCells[cellIndex], out value))
                                    row.Cells[cellIndex + 1].Range.Text = value.ToString();
                            }
                        }
                    }
                    DocumentSaveAs(doc, destination);
                }
                finally
                {
                    CloseDocuments(app.Documents);
                }
            }
            finally
            {
                app.Quit();
                System.Runtime.InteropServices.Marshal.ReleaseComObject(app);
            }
        }

        private static readonly Regex CellRegex = new Regex(@"[\s\n\r\a\t]");

        public static void InsertTableAndReplaceText<TKey, TValue>(string templateName, string destination, IList<IDictionary<string, TValue>> body, IDictionary<TKey, TValue> replaceDict, string picturePath = null)
        {
            if (!File.Exists(templateName))
            {
                ShowFileNotFound(templateName);
                return;
            }
            var app = new Application();
            try
            {
                var doc = OpenDocument(app, templateName);
                if (doc == null) return;
                try
                {
                    var s = app.Selection;
                    s.Find.ClearFormatting();
                    s.Find.Replacement.ClearFormatting();

                    if (body.Count > 0)
                    {
                        var keys = body[0].Select(p => p.Key).ToList();

                        var tableCount = doc.Tables.Count;
                        for (int tableIndex = 1; tableIndex <= tableCount; tableIndex++)
                        {
                            var table = doc.Tables[tableIndex];

                            int rowIndex = -1;
                            try
                            {
                                for (var col = 1; col <= table.Columns.Count; col++)
                                {
                                    for (int row = 1; row <= table.Rows.Count; row++)
                                    {
                                        var text = table.Columns[col].Cells[row].Range.Text;
                                        //text = CellRegex.Replace(text, "");

                                        if (keys.Any(k => text.Contains(k)))
                                        {
                                            rowIndex = row;
                                            goto out_of_cycle;
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                App.Logger.Warn(e.Message);
                            }
                            out_of_cycle:

                            if (rowIndex == -1) continue;

                            var replaceRow = table.Rows[rowIndex];
                            string[] headerCells = replaceRow
                                .Cells
                                .Cast<Cell>()
                                //.Select(c => CellRegex.Replace(c.Range.Text, ""))
                                .Select(c => c.Range.Text)
                                .ToArray();

                            foreach (var dict in body)
                            {
                                var row = table.Rows.Add();
                                for (int cellIndex = 0; cellIndex < headerCells.Length; cellIndex++)
                                {
                                    var sb = new StringBuilder(headerCells[cellIndex]);
                                    foreach (var pair in dict)
                                    {
                                        sb.Replace(pair.Key, pair.Value?.ToString());
                                    }
                                    var res = sb.ToString();
                                    if (headerCells[cellIndex] != res)
                                        row.Cells[cellIndex + 1].Range.Text = res;
                                    //TValue value;
                                    //if (dict.TryGetValue(headerCells[cellIndex], out value))
                                    //    row.Cells[cellIndex + 1].Range.Text = value.ToString();
                                }
                            }
                            replaceRow.Delete();
                        }
                    }
                    if (replaceDict != null)
                        foreach (var pair in replaceDict)
                        {
                            s.Find.Execute(FindText: pair.Key, ReplaceWith: pair.Value, MatchCase: false, Replace: WdReplace.wdReplaceAll);
                        }
                    if (picturePath != null)
                    {
                        ReplaceByPicture(s, picturePath, "&img");
                    }
                    DocumentSaveAs(doc, destination);
                }
                finally
                {
                    CloseDocuments(app.Documents);
                }
            }
            finally
            {
                app.Quit();
                System.Runtime.InteropServices.Marshal.ReleaseComObject(app);
            }
        }

        public static void ReplaceWithDuplicate<TKey, TValue>(string templateName, string destination, IDictionary<TKey, TValue>[] data, string picturePath = null)
        {
            if (!File.Exists(templateName))
            {
                ShowFileNotFound(templateName);
                return;
            }
            var app = new Application();
            try
            {
                var doc = OpenDocument(app, templateName);
                if (doc == null) return;
                try
                {
                    var s = app.Selection;
                    s.Find.ClearFormatting();
                    s.Find.Replacement.ClearFormatting();

                    s.WholeStory();
                    s.Copy();
                    int pos = 0;

                    for (int i = 0; i < data.Length; i++)
                    {
                        s.Paste();
                        s.SetRange(pos, s.Range.End);

                        foreach (var pair in data[i])
                        {
                            s.Find.Execute(FindText: pair.Key, ReplaceWith: pair.Value, MatchCase: false, Replace: WdReplace.wdReplaceAll);
                        }

                        pos = s.Range.End;
                        s.SetRange(pos, pos);
                    }

                    if (picturePath != null)
                        ReplaceByPicture(s, picturePath, "&img");
                    DocumentSaveAs(doc, destination);
                }
                finally
                {
                    CloseDocuments(app.Documents);
                }
            }
            finally
            {
                app.Quit();
                System.Runtime.InteropServices.Marshal.ReleaseComObject(app);
            }
        }

        private static void ReplaceByPicture(Selection s, string picturePath, string placeOf)
        {
            s.Find.Execute(placeOf);
            var imgRange = s.Range;
            if (imgRange.Text != null)
            {
                s.InlineShapes.AddPicture(picturePath, Range: imgRange);
                s.Find.Execute(placeOf, ReplaceWith: string.Empty, MatchCase: false, Replace: WdReplace.wdReplaceAll);
            }
        }

        // Helpers

        private static void ShowFileNotFound(string fileName)
        {
            MessageBox.Show($"Внимание, шаблон \"{fileName}\" не найден!", "Файл не найден", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static Document OpenDocument(Application app, string templateName)
        {
            try
            {
                return app.Documents.Open(templateName);
            }
            catch (Exception e)
            {
                App.Logger.Info(e, "Cannot open: " + templateName);
                MessageBox.Show("Не удалось открыть файл: " + templateName + "\n\nСообщение об ошибке: " + e.Message, "Ошибка открытия файла", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }

        private static void DocumentSaveAs(Document doc, string file)
        {
            var dir = Path.GetDirectoryName(file);
            // ReSharper disable once AssignNullToNotNullAttribute
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            try
            {
                doc.SaveAs(FileName: file);
            }
            catch (Exception e)
            {
                App.Logger.Info(e, "Cannot save: " + file);
                MessageBox.Show("Не удалось сохранить файл: " + file + "\n\nСообщение об ошибке: " + e.Message, "Ошибка сохранения файла", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static void CloseDocuments(Documents documents)
        {
            documents.Close(SaveChanges: WdSaveOptions.wdDoNotSaveChanges);
        }
    }
}
