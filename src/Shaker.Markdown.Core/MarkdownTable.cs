using System.Collections.Generic;

namespace Shaker.Markdown.Core
{
    /// <summary>One table found in a document, flattened to text.</summary>
    /// <remarks>
    /// Cells keep their words and lose their formatting: <c>**Total**</c> comes back as <c>Total</c>. A
    /// table being read into data is being read for its values, and markup in a column header would only
    /// have to be stripped again by whoever consumes it.
    /// </remarks>
    public sealed class MarkdownTable
    {
        /// <summary>Creates a table.</summary>
        public MarkdownTable(IList<string> headers, IList<IList<string>> rows)
        {
            Headers = headers ?? new List<string>();
            Rows = rows ?? new List<IList<string>>();
        }

        /// <summary>The column names, from the table's header row. Empty when it has none.</summary>
        public IList<string> Headers { get; }

        /// <summary>The body rows, each as many cells as the row actually had.</summary>
        public IList<IList<string>> Rows { get; }

        /// <summary>How many columns the widest row has, headers included.</summary>
        public int ColumnCount
        {
            get
            {
                int widest = Headers.Count;

                foreach (IList<string> row in Rows)
                {
                    if (row.Count > widest)
                        widest = row.Count;
                }

                return widest;
            }
        }
    }
}
