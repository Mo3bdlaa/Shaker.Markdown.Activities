using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Shaker.Markdown.Activities.Design
{
    /// <summary>
    /// Works out which folder a note's relative file path is relative to — the UiPath project folder.
    /// </summary>
    /// <remarks>
    /// Studio knows the answer and does not offer it to a designer through any documented API, so this asks
    /// the editing context for the workflow file currently open and walks up from it to the
    /// <c>project.json</c> beside it. All of it by reflection, and all of it optional: when the project
    /// cannot be found the note falls back to the process's working directory, and an absolute path never
    /// needed any of this in the first place.
    /// </remarks>
    internal static class ProjectLocator
    {
        /// <summary>Resolves a note's file path to something that can be opened, or null.</summary>
        /// <param name="designer">The designer asking, used to reach the editing context.</param>
        /// <param name="path">The path from the activity. May be absolute or relative.</param>
        internal static string Resolve(object designer, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                if (Path.IsPathRooted(path))
                    return Path.GetFullPath(path);

                string root = FindProjectFolder(designer) ?? Directory.GetCurrentDirectory();
                return Path.GetFullPath(Path.Combine(root, path));
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown note could not resolve the path " + path + ": " + exception);
                return null;
            }
        }

        /// <summary>The folder holding <c>project.json</c>, or null when it cannot be found.</summary>
        internal static string FindProjectFolder(object designer)
        {
            string workflow = FindOpenWorkflowFile(designer);

            if (string.IsNullOrEmpty(workflow))
                return null;

            try
            {
                var folder = new DirectoryInfo(Path.GetDirectoryName(workflow) ?? string.Empty);

                // A workflow can sit several folders deep inside a project; the project is the first ancestor
                // carrying a project.json, and the walk stops at the drive root either way.
                while (folder != null && folder.Exists)
                {
                    if (File.Exists(Path.Combine(folder.FullName, "project.json")))
                        return folder.FullName;

                    folder = folder.Parent;
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown note could not walk up to the project folder: " + exception);
            }

            return null;
        }

        /// <summary>
        /// The path of the workflow the designer belongs to, taken from the editing context's
        /// <c>WorkflowFileItem</c>.
        /// </summary>
        private static string FindOpenWorkflowFile(object designer)
        {
            try
            {
                object context = designer?.GetType()
                    .GetProperty("Context", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(designer, null);

                object items = context?.GetType()
                    .GetProperty("Items", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(context, null);

                if (items == null)
                    return null;

                Type fileItem = Type.GetType(
                    "System.Activities.Presentation.WorkflowFileItem, System.Activities.Presentation");

                if (fileItem == null)
                    return null;

                MethodInfo getValue = items.GetType()
                    .GetMethod("GetValue", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type) }, null);

                object item = getValue?.Invoke(items, new object[] { fileItem });

                return item?.GetType()
                    .GetProperty("LoadedFile", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(item, null) as string;
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown note could not find the open workflow file: " + exception);
                return null;
            }
        }
    }
}
