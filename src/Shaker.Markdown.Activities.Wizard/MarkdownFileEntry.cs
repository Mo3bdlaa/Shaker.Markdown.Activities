using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Shaker.Markdown.Activities.Wizard
{
    /// <summary>One <c>.md</c> file offered in the viewer's list.</summary>
    internal sealed class MarkdownFileEntry
    {
        internal MarkdownFileEntry(string fullPath, string relativePath)
        {
            FullPath = fullPath;
            RelativePath = relativePath;
        }

        /// <summary>Where the file is.</summary>
        internal string FullPath { get; }

        /// <summary>What to call it in the list — its path within the project.</summary>
        internal string RelativePath { get; }

        /// <inheritdoc />
        public override string ToString() => RelativePath;

        /// <summary>
        /// Folders that are never worth walking: build output, version control, and the working folders
        /// Studio keeps beside a project.
        /// </summary>
        private static readonly HashSet<string> Skipped = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".git", ".svn", ".vs", ".idea", ".local", ".settings", ".objects", ".screenshots",
            ".templates", ".tmh", "bin", "obj", "node_modules", "packages"
        };

        /// <summary>Finds the Markdown files in a project, nearest the root first.</summary>
        internal static IList<MarkdownFileEntry> Find(string root)
        {
            var found = new List<MarkdownFileEntry>();

            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                return found;

            Walk(new DirectoryInfo(root), root, found, depth: 0);

            // Shallow before deep, then alphabetical: a project's own README should be the first thing in
            // the list rather than buried among whatever a dependency folder happens to contain.
            return found
                .OrderBy(entry => entry.RelativePath.Count(c => c == Path.DirectorySeparatorChar))
                .ThenBy(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void Walk(DirectoryInfo folder, string root, List<MarkdownFileEntry> found, int depth)
        {
            // A guard against a pathological tree, and against a symlink loop, rather than a real limit:
            // no project keeps its documentation twelve folders down.
            if (depth > 12 || found.Count > 500)
                return;

            try
            {
                foreach (FileInfo file in folder.GetFiles("*.md"))
                    found.Add(new MarkdownFileEntry(file.FullName, Relative(root, file.FullName)));

                foreach (DirectoryInfo child in folder.GetDirectories())
                {
                    if (Skipped.Contains(child.Name) || child.Attributes.HasFlag(FileAttributes.ReparsePoint))
                        continue;

                    Walk(child, root, found, depth + 1);
                }
            }
            catch (Exception exception)
            {
                // A folder that cannot be read is skipped rather than failing the whole listing.
                Debug.WriteLine("Markdown viewer could not read " + folder.FullName + ": " + exception);
            }
        }

        private static string Relative(string root, string path)
        {
            if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return path.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return Path.GetFileName(path);
        }
    }
}
