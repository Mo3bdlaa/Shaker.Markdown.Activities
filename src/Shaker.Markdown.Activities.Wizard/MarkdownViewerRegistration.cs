using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Wizards;

namespace Shaker.Markdown.Activities.Wizard
{
    /// <summary>
    /// Puts a <c>Markdown</c> button in Studio's ribbon, opening a viewer for the project's <c>.md</c> files.
    /// </summary>
    /// <remarks>
    /// Studio looks for implementations of <see cref="IRegisterWorkflowDesignApi"/> when it loads the
    /// package and calls <see cref="Initialize"/> once.
    /// </remarks>
    public sealed class MarkdownViewerRegistration : IRegisterWorkflowDesignApi
    {
        /// <summary>
        /// Finds the ribbon button's icon.
        /// </summary>
        /// <remarks>
        /// A pack URI is the obvious way to name an image inside an assembly, and it does not work here:
        /// resolving one asks WPF to load this assembly by name, which fails when Studio has loaded it into
        /// a context of its own. So the icon is shipped as a file beside this assembly and named by its path,
        /// which needs nothing resolved. The copy embedded in the assembly is kept as a fallback for a host
        /// that can resolve a pack URI after all.
        /// </remarks>
        private static string FindIcon()
        {
            const string embedded = "/Shaker.Markdown.Activities.Wizard;component/Resources/markdown.png";

            try
            {
                string assembly = typeof(MarkdownViewerRegistration).Assembly.Location;
                if (string.IsNullOrEmpty(assembly))
                    return embedded;

                string beside = Path.Combine(Path.GetDirectoryName(assembly) ?? string.Empty, "markdown.png");
                return File.Exists(beside) ? new Uri(beside).AbsoluteUri : embedded;
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown viewer icon could not be located: " + exception);
                return embedded;
            }
        }

        /// <summary>Registers the wizard.</summary>
        public void Initialize(IWorkflowDesignApi api)
        {
            try
            {
                string project = FindProjectFolder(api);

                var wizards = new WizardCollection();

                wizards.WizardDefinitions.Add(new WizardDefinition
                {
                    DisplayName = "Markdown",
                    IconUri = FindIcon(),
                    Tooltip = "Browse and read the Markdown files in this project — the README, the " +
                              "process documentation, anything with a .md extension — rendered rather than as source.",
                    MinimizeBeforeRun = false,
                    Wizard = new WizardBase { RunWizard = () => Open(project) }
                });

                api.Wizards.Register(wizards);
            }
            catch (Exception exception)
            {
                // Never let a wizard cost the package its activities.
                Debug.WriteLine("Markdown viewer could not be registered: " + exception);
            }
        }

        /// <summary>
        /// Opens the viewer. The wizard contract expects an activity to drop on the canvas; this one only
        /// shows documents, so it returns nothing.
        /// </summary>
        private static System.Activities.Activity Open(string project)
        {
            try
            {
                new MarkdownViewerWindow(project).ShowDialog();
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown viewer failed: " + exception);
            }

            return null;
        }

        /// <summary>
        /// Asks Studio where the open project is.
        /// </summary>
        /// <remarks>
        /// By reflection over the project properties service, because the property that carries the folder
        /// is not the same on every Studio the package has to load in, and none of it is worth failing the
        /// registration over. When it cannot be found the viewer opens with an empty list and an Open file
        /// button, which is a usable window rather than a missing one.
        /// </remarks>
        private static string FindProjectFolder(IWorkflowDesignApi api)
        {
            try
            {
                object service = api?.ProjectPropertiesService;

                if (service == null)
                    return null;

                foreach (string name in new[] { "ProjectDirectory", "ProjectFolder", "ProjectPath", "Directory" })
                {
                    if (!(service.GetType()
                            .GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
                            ?.GetValue(service, null) is string value))
                        continue;

                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    // Some of these carry the project.json rather than the folder holding it.
                    string folder = File.Exists(value) ? Path.GetDirectoryName(value) : value;

                    if (Directory.Exists(folder))
                        return folder;
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown viewer could not find the project folder: " + exception);
            }

            return null;
        }
    }
}
