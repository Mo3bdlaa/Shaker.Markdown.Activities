using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using Shaker.Markdown.Core;

namespace Shaker.Markdown.Activities
{
    /// <summary>Reads a Markdown table into a <see cref="DataTable"/>.</summary>
    [Category(Categories.Markdown)]
    [DisplayName("Get DataTable From Markdown")]
    [Description("Reads a Markdown table into a DataTable, ready for For Each Row or Write Range.")]
    public sealed class GetDataTableFromMarkdown : MarkdownActivityBase<DataTable>
    {
        /// <summary>The document holding the table.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("Markdown")]
        [Description("The Markdown containing the table. A whole README is fine; use Table index to pick which table.")]
        public InArgument<string> Markdown { get; set; }

        /// <summary>Which table to read, when the document holds more than one.</summary>
        [Category(Categories.Input)]
        [DisplayName("Table index")]
        [Description("Which table to read, counting from 0 in the order they appear. Leave blank for the first one.")]
        public InArgument<int> TableIndex { get; set; }

        /// <summary>
        /// Whether the table's header row becomes the column names.
        /// </summary>
        /// <remarks>
        /// On by default, because a Markdown table always has a header row — the syntax requires the
        /// <c>---</c> separator under it. Turning it off gives generic column names and keeps the header as
        /// the first row of data, which is what you want when the "header" is really the first record.
        /// </remarks>
        [Category(Categories.Options)]
        [DisplayName("First row is header")]
        [Description("Use the table's header row as the column names. Turn off to get Column1, Column2 and keep the header as data.")]
        public InArgument<bool> FirstRowIsHeader { get; set; } = new InArgument<bool>(true);

        /// <summary>How many tables the document holds, whichever one was asked for.</summary>
        [Category(Categories.Output)]
        [DisplayName("Table count")]
        [Description("How many tables the document holds. Useful for checking a document had the table you expected before reading it.")]
        public OutArgument<int> TableCount { get; set; }

        /// <inheritdoc />
        protected override DataTable Execute(CodeActivityContext context)
        {
            IList<MarkdownTable> tables = MarkdownEngine.GetTables(
                Markdown.GetValue(context), ReadOptions(context));

            TableCount.SetValue(context, tables.Count);

            int index = TableIndex.GetValue(context);

            if (index < 0 || index >= tables.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(TableIndex),
                    index,
                    tables.Count == 0
                        ? "This Markdown contains no tables. Note that tables need the Advanced flavor, which is the default."
                        : "This Markdown contains " + tables.Count + " table(s), so the index must be between 0 and " + (tables.Count - 1) + ".");
            }

            return ToDataTable(tables[index], FirstRowIsHeader.GetValue(context));
        }

        /// <summary>Builds the DataTable, every column a string.</summary>
        /// <remarks>
        /// Strings rather than guessed types. A Markdown table carries no type information, and a column of
        /// order numbers that happens to look numeric would come back as an integer in one document and a
        /// string in the next depending on its contents — which is a worse problem to debug than converting
        /// a column yourself.
        /// </remarks>
        private static DataTable ToDataTable(MarkdownTable table, bool headerIsColumnNames)
        {
            var result = new DataTable { Locale = CultureInfo.InvariantCulture };
            int columns = table.ColumnCount;

            for (int column = 0; column < columns; column++)
            {
                string name = headerIsColumnNames && column < table.Headers.Count
                    ? table.Headers[column]
                    : null;

                result.Columns.Add(UniqueName(result, name, column), typeof(string));
            }

            // The header is data too when it is not being used as the column names.
            if (!headerIsColumnNames && table.Headers.Count > 0)
                AddRow(result, table.Headers, columns);

            foreach (IList<string> row in table.Rows)
                AddRow(result, row, columns);

            return result;
        }

        private static void AddRow(DataTable table, IList<string> cells, int columns)
        {
            DataRow row = table.NewRow();

            // A ragged row is padded rather than rejected: Markdown lets a row be short, and losing the
            // cells it does have would be the worse answer.
            for (int column = 0; column < columns; column++)
                row[column] = column < cells.Count ? cells[column] : string.Empty;

            table.Rows.Add(row);
        }

        /// <summary>
        /// A column name the table does not already have — Markdown is happy to repeat a header, and
        /// <see cref="DataTable"/> is not.
        /// </summary>
        private static string UniqueName(DataTable table, string name, int column)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "Column" + (column + 1);

            if (!table.Columns.Contains(name))
                return name;

            for (int suffix = 2; ; suffix++)
            {
                string candidate = name + suffix;

                if (!table.Columns.Contains(candidate))
                    return candidate;
            }
        }
    }
}
