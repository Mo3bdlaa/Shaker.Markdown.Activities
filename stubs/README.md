# Reference stubs

Studio supplies the workflow designer assemblies itself, and they are not published anywhere: no NuGet feed
carries `System.Activities.Presentation` or `System.Activities.Metadata` for .NET. Compiling against them
still needs *something* with the right identity, so these two projects stand in for them.

They contain only the handful of types and members the designers here actually touch, with the identities
Studio loads — name, `6.0.0.0`, unsigned:

```
System.Activities.Metadata      6.0.0.0   ModelItem, ModelProperty, AttributeTable(Builder), MetadataStore, IRegisterMetadata
System.Activities.Presentation  6.0.0.0   WorkflowViewElement, ActivityDesigner
```

.NET Framework keeps all of these in `System.Activities.Presentation`, while the .NET port splits the
metadata half into its own assembly. That is why one designer cannot be built for both from one set of
references, and why a .NET Framework designer never loaded in a modern Studio.

**Neither stub is shipped.** They are referenced with `Private=false` so they stay out of the package, and
the real assemblies are the ones present at run time. Keep the member signatures identical to the real ones:
a mismatch compiles happily here and fails only when Studio loads the designer.

That last risk is why the annotation reader in `Shaker.Markdown.Activities.Design` goes through reflection
rather than binding to `ModelItem.Properties` at compile time. Annotation text is not part of any contract
UiPath documents, so the reader probes for it and returns null when it cannot find it, instead of taking the
designer down with it.
