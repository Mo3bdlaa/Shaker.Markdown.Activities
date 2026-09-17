# Built packages

The activity package, built and checked in so it can be installed into Studio without building anything.

| | |
| --- | --- |
| File | `Shaker.Markdown.Activities.1.0.0.nupkg` |
| Version | 1.0.0 |
| Built from | `d0ed7366698db905b78e2859c70c0175a1ef2a73` |
| SHA-256 | `c8476a311848cf28f93259801d1220a75bed2715aa833d59edb140b624908345` |
| Size | 588K |

## Installing it into Studio

1. Copy `Shaker.Markdown.Activities.1.0.0.nupkg` into a folder. A network share works well for a team.
2. In Studio, open **Manage Packages → Settings** and add that folder as a user-defined package source.
3. Find **Shaker.Markdown.Activities** under that source and install it.

To publish it to Orchestrator instead, upload the same file to a tenant feed.

Reinstalling over the same version number is the one thing to watch: Studio and the project's own
`packages` folder both cache by version, so uninstall the old copy and delete it from the project before
installing this one, or you will be looking at the previous build.

## What is inside

```
lib/net461/              Shaker.Markdown.Activities.dll          the activities
                         Shaker.Markdown.Core.dll                the Markdown engine
                         Shaker.Markdown.Activities.Design.dll   the canvas designer, panel folder, icons
                         Shaker.Markdown.Activities.Windows.dll  Show Markdown, Markdown To PDF
                         …Windows.Design.dll                     their panel folder and icons
                         Microsoft.Web.WebView2.*                the browser those two render through

lib/net6.0-windows7.0/   the same set, built for .NET

lib/net6.0/              Shaker.Markdown.Activities.dll          the activities
                         Shaker.Markdown.Core.dll                the Markdown engine
```

`net461` covers Windows-legacy projects, `net6.0-windows7.0` covers Windows ones, and `net6.0` covers
cross-platform ones.

**Which folder Studio resolves is load-bearing.** A Windows project resolves `lib/net6.0-windows7.0`, so
designers placed only in `lib/net6.0` are never loaded — and a package whose design assembly never loads
shows its activities in a folder named after the assembly, with no icons and no rendered note. Offering all
three folders is what works. `lib/net6.0` stays free of WPF because it is also what a Linux robot resolves.

## Why there are almost no dependencies

Studio and the Robot supply the workflow runtime themselves, so it is referenced at compile time only and
never declared. WebView2 and the Studio API are carried as files beside the assemblies that need them, so a
cross-platform project installing this package has neither restored into it. Markdig is the one real
dependency, declared rather than embedded because it is somebody else's library under its own licence.

The version the activities are compiled against matters just as much. They ask the host for
`System.Activities 6.0.0.0`, which is what Studio ships; the runtime resolves an assembly forward but never
backward, so building against a higher version makes every activity fail to load with *Could not load file
or assembly 'System.Activities'*. That is why the workflow runtime is taken from UiPath's official feed
rather than from nuget.org, whose `UiPath.Workflow` builds carry `6.0.3.0` and are not what any Studio has.

## Rebuilding it

```bash
dotnet build Shaker.Markdown.Activities.sln -c Release
dotnet pack  src/Shaker.Markdown.Activities/Shaker.Markdown.Activities.csproj -c Release -o packages
```

Build the solution first: the designers and the Windows activities are collected from their own build
output, and packing without them warns and produces a package that installs and then does nothing visible
in Studio.

Every push also builds this package on CI and attaches it to the run, which is the copy to prefer if this
one is ever behind the source.
