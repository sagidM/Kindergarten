using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Office.Interop.Word;

namespace WpfApp.Util
{
    public class WordWorker
    {
        public static void Replace<TKey, TValue>(string templateName, string destination, IDictionary<TKey, TValue> data, string picturePath = null)
        {
            var app = new Application();
            try
            {
                var doc = app.Documents.Open(templateName);
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
                    app.Documents.Close();
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
            var app = new Application();
            try
            {
                var doc = app.Documents.Open(templateName);
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
                            .Select(c => Regex.Replace(c.Range.Text, "[\\s\\n\\r\\a\\t]", ""))
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
                    app.Documents.Close();
                }
            }
            finally
            {
                app.Quit();
                System.Runtime.InteropServices.Marshal.ReleaseComObject(app);
            }
        }

        private static void DocumentSaveAs(Document doc, string file)
        {
            var dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            doc.SaveAs(FileName: file);
        }

        public static void InsertTableAndReplaceText<TKey, TValue>(string templateName, string destination, IList<IDictionary<string, TValue>> body, IDictionary<TKey, TValue> replaceDict, string picturePath = null)
        {
            var app = new Application();
            try
            {
                var doc = app.Documents.Open(templateName);
                try
                {
                    var s = app.Selection;
                    s.Find.ClearFormatting();
                    s.Find.Replacement.ClearFormatting();

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
                                    text = Regex.Replace(text, @"[\s\n\r\a\t]", "");

                                    foreach (var line in body)
                                    {
                                        if (line.ContainsKey(text))
                                        {
                                            rowIndex = row;
                                            goto out_of_cycle;
                                        }
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
                        var headerCells = replaceRow
                            .Cells
                            .Cast<Cell>()
                            .Select(c => Regex.Replace(c.Range.Text, "[\\s\\n\\r\\a\\t]", ""))
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
                        replaceRow.Delete();
                    }
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
                    app.Documents.Close();
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
            var app = new Application();
            try
            {
                var doc = app.Documents.Open(templateName);
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
                    app.Documents.Close();
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
    }
}
