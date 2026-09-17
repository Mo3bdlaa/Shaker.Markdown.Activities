using System.Windows;

// Points WPF at Themes/Generic.xaml in this assembly as the dictionary to fall back to when a resource is
// not found anywhere nearer. That is the route an icon lookup takes when the host does not load
// Themes/Icons.xaml by pack URI itself, and Generic.xaml merges Icons.xaml so both routes end up at the
// same brushes.
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,          // no per-theme dictionaries: the icons are theme independent
    ResourceDictionaryLocation.SourceAssembly // the generic dictionary lives in this assembly
)]
