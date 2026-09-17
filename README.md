# Shaker Markdown Activities

Markdown inside UiPath. A **Markdown Note** that Studio draws as a formatted document on the canvas instead
of as source, a **Markdown** button in the ribbon for reading the project's `.md` files, and activities that
turn Markdown into HTML, plain text, a DataTable or a PDF while a process runs.

One package, `Shaker.Markdown.Activities`. Every activity in it appears under **Markdown** in the Activities
panel — one folder, at the top level, whatever the dependency list calls the package.

![The Markdown mark](src/Shaker.Markdown.Activities/package-icon.png)

## What this can and cannot do

Studio's extensibility runs through two doors: activities, and the WPF designers that draw them. Everything
here goes through one of those, which decides what was possible and what was not.

| | |
|---|---|
| Render Markdown you type into an activity, on the canvas | **Yes.** A custom `ActivityDesigner` hosting a `FlowDocument`. |
| Render an activity's **own annotation** as Markdown | **Yes**, on a `Markdown Note`. Set `Source` to `Annotation`. |
| Re-render the annotation bubbles on **other** activities | **No.** Studio draws that chrome itself and exposes no hook. |
| Replace the built-in **Comment** activity's rendering | **No.** `UiPath.Core.Activities.Comment` is sealed and its designer is fixed. Use `Markdown Note`, which is the same idea with a renderer. |
| Render a `.md` file **on the canvas** | **Yes.** Set `Source` to `File` and point it at a file in the project. |
| Open a `.md` file as a **Studio editor tab** | **No.** `IWorkflowDesignApi` offers wizards, settings and analyzer rules — nothing that owns a file type or an editor tab. |
| Browse and read the project's `.md` files | **Yes**, through the ribbon button, which is the closest a package can get to the row above. |
| Convert Markdown at run time | **Yes.** To HTML, to plain text, to a DataTable, to a PDF. |
| Show a rendered document to a person mid-process | **Yes**, from the Windows package. |

## Activities

| Activity | What it does |
|---|---|
| **Markdown Note** | Documentation drawn on the canvas, rendered. Does nothing at run time. Takes its text from its own `Markdown` property, from its annotation, or from a `.md` file in the project. |
| **Markdown To HTML** | Renders Markdown to an HTML fragment for an email body, or to a complete styled page for a file or a browser control. |
| **Markdown To Text** | Strips the markup and leaves the words, for a log line or a subject line. |
| **Read Markdown File** | Reads a `.md` file and reports its text, its first heading and the folder it came from. |
| **Get Markdown Outline** | Lists a document's headings in order, each with its level, text and anchor. |
| **Get DataTable From Markdown** | Reads a Markdown table into a DataTable, ready for For Each Row or Write Range. |

`Markdown Note` is the only one that needs a designer. The rest are ordinary activities and work the same in
a Windows project, a Windows-legacy project and a cross-platform one.

### The two that need a screen

| Activity | What it does |
|---|---|
| **Show Markdown** | Puts a rendered document on screen and waits until the window is closed. Attended machines only. |
| **Markdown To PDF** | Renders a document and prints it to a PDF file, portrait or landscape. |

These ship in the same package but in a Windows-only assembly, so a cross-platform project gets neither them
nor the browser behind them. Both work in a Windows project and a Windows-legacy one.

Both render through WebView2, so the **Microsoft Edge WebView2 Evergreen Runtime** has to be installed on
the machine that runs them. There is no fallback renderer here on purpose: these two exist to show a
document faithfully and to print one, and a fallback would do the first badly and the second not at all — so
a missing runtime is reported with the name of what to install.

Both run their window on a thread of their own with a message pump, because a Robot promises neither a
single-threaded apartment nor a running pump, and WPF needs both. Both take a `Timeout (seconds)`, so a
window nobody closes fails the activity rather than hanging the process.

### Writing a note

`Markdown` and `File path` are ordinary arguments, so both take variables and expressions. The canvas can
only draw what it can read without running the workflow, so a literal is rendered and an expression is
reported as one — the note says which it is rather than appearing blank.

Turn on **Show editor** and the card grows a multi-line editor above the rendered document. The preview
follows the typing; the activity is written when the editor loses focus, so one edit is one undo step rather
than one per keystroke.

## Project types

| Project type | Activities | Rendered note on the canvas | Ribbon viewer | Show Markdown / To PDF |
|---|---|---|---|---|
| Windows (.NET 8) | Yes | Yes | Yes | Yes |
| Windows – Legacy (.NET Framework) | Yes | Yes | No | Yes |
| Cross-platform | Yes | No — shown in the properties panel instead | No | No |

The designers are WPF, so they exist only where Studio is. A cross-platform project still gets every
activity and a `Markdown Note` still carries its text; what it loses is the drawing. That is why the
package ships three sets of assemblies:

```
lib/net461/   activities, engine, .NET Framework designers,
              Show Markdown + Markdown To PDF, WebView2       → Windows-legacy Studio
lib/net6.0/   activities, engine, .NET designers, the viewer,
              Show Markdown + Markdown To PDF, WebView2       → modern Studio, and the robot
```

One WebView2 build serves both, pinned to `1.0.2478.35` — later releases dropped their .NET Framework
assets, and two versions in one package would mean a Windows-legacy build running against an assembly that
is not the one beside it. CI asserts the whole layout.

## Rendering

Two renderers, because the two places have different needs.

**On the canvas** — a `FlowDocument`, built by `FlowDocumentMarkdownRenderer`. The canvas redraws a note on
every keystroke and a workflow may hold a dozen of them, so a browser control per note would cost a browser
process each. Headings, emphasis, lists, task lists, quotes, code blocks, tables, links, footnotes and local
images all render. Raw HTML and syntax highlighting do not.

**In the viewer window** — WebView2, for full fidelity, falling back to the same `FlowDocument` renderer when
the Evergreen runtime is not installed. The status bar says which one drew what you are looking at. WebView2's
assemblies travel beside the viewer rather than as a package dependency, so nothing is restored into your
automation project.

### Raw HTML is off by default

`AllowHtml` is off everywhere, and that is a decision rather than an oversight. Markdown reaching this
library comes from files in a repository or from variables filled at run time, and raw HTML in either can
carry script. The viewer additionally serves pages under a `Content-Security-Policy` that forbids script
outright, disables WebView2's script engine, and reaches the disk through a virtual host mapped read-only to
the one folder the document came from. Links open only `http`, `https` and `mailto`, so a document cannot
launch a local executable through a `file://` link. Turn `AllowHtml` on for documents you wrote.

## Building

The whole solution builds on Linux and macOS as well as Windows — `EnableWindowsTargeting` covers the WPF
projects — so CI needs nothing but the .NET 8 SDK.

```bash
dotnet build Shaker.Markdown.Activities.sln -c Release
dotnet test  Shaker.Markdown.Activities.sln -c Release
dotnet pack  src/Shaker.Markdown.Activities/Shaker.Markdown.Activities.csproj -c Release -o artifacts
```

Pack after a full solution build, not on its own: the designers and the viewer are pulled into the package
from their own output folders, and packing without them succeeds with a warning rather than failing.

### Layout

```
src/Shaker.Markdown.Core               netstandard2.0     parsing and rendering, no UiPath and no WPF
src/Shaker.Markdown.Activities         net461;net6.0      the activities
src/Shaker.Markdown.Activities.Design  net461;net6.0-win  the canvas designers
src/Shaker.Markdown.Activities.Wizard  net6.0-windows     the ribbon viewer
src/…Activities.Windows                net461;net6.0-win  Show Markdown and Markdown To PDF
src/…Activities.Windows.Design         net461;net6.0-win  their panel folder and icons
stubs/                                 net6.0-windows     stand-ins for the designer assemblies Studio owns
tests/                                 net8.0             the engine's tests
tools/make-icon.py                                        regenerates the icons
```

`stubs/` needs a word of explanation: no NuGet feed publishes `System.Activities.Presentation` or
`System.Activities.Metadata` for .NET, and Studio supplies them itself at run time. The stubs stand in at
compile time with the identities Studio loads, and are never shipped. See `stubs/README.md`.

## Two Studio conventions worth knowing

Both of these are how Studio actually behaves rather than how it looks like it should, and both cost this
repo a round trip.

**The Activities panel folder comes from the attribute table, not from the activity class.** A
`[Category("Markdown")]` on the class is read for the *properties* panel and ignored for the Activities
panel, which without other instruction groups activities by package id — and splits it on the dots, so
`Shaker.Markdown.Activities` became *Shaker ▸ Markdown*. The folder is set by registering the attribute in
`IRegisterMetadata`:

```csharp
builder.AddCustomAttributes(typeof(MarkdownNote), new CategoryAttribute("Markdown"));
```

Dots nest there too, so one name with no dots is one folder at the top level. This is registered for every
activity in `DesignerMetadata` and `WindowsDesignerMetadata`.

**Icons come from `Themes/Icons.xaml`, not from the designer.** Setting `ActivityDesigner.Icon` in a
designer's constructor does not put an icon in the Activities panel. Studio looks up a `DrawingBrush` by the
key `<ActivityName>Icon` in the design assembly, so the icons live in
`src/Shaker.Markdown.Activities.Design/Themes/Icons.xaml` and its Windows counterpart, keyed by activity type
name. A renamed activity silently loses its icon, so CI checks that every activity still has one.

Because Studio applies those icons by name, the converters keep UiPath's own card rather than a custom
designer — only the note needs one, and it needs one for the rendering, not the icon.

## Notes on the parts that are not contracts

Two things here lean on behaviour UiPath does not document, and both are written to fail quietly rather than
take the package down with them:

- **Reading an activity's annotation.** Annotation text lives in the attached property
  `sap2010:Annotation.AnnotationText`, which is not part of any published API. `AnnotationReader` probes for
  it by reflection and returns null when it cannot find it, so a `Markdown Note` set to `Annotation` shows a
  hint instead of breaking.
- **Reading a literal out of an argument.** The card's editor needs the text behind an `InArgument<string>`
  before anything has run. A `Literal<string>` is read directly; a VB or C# expression that is nothing but a
  quoted string is unquoted; anything else is left alone and reported as an expression.
- **Finding the project folder.** Needed to resolve a note's relative file path and to list files for the
  viewer. Taken from the editing context and the project properties service by reflection, falling back to
  the working directory on the canvas and to an *Open file* button in the viewer.

## Licence

MIT. Markdown parsing is [Markdig](https://github.com/xoofx/markdig) by Alexandre Mutel, BSD-2-Clause.
