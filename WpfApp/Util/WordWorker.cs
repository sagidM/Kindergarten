using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Office.Interop.Word;

namespace WpfApp.Util
{
    public class WordWorker
    {
        public static void Replace<TKey, TValue>(string templateName, string destination, IDictionary<TKey, TValue> data)
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
                    doc.SaveAs(FileName: destination);
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
                    doc.SaveAs(FileName: destination);
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

        public static void InsertTableAndReplaceText<TKey, TValue>(string templateName, string destination, IDictionary<string, TValue>[] body, IDictionary<TKey, TValue> replaceDict)
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
                    doc.SaveAs(FileName: destination);
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
    }
}
