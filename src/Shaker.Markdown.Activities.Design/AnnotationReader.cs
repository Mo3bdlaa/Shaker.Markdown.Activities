using System;
using System.Diagnostics;
using System.Reflection;

namespace Shaker.Markdown.Activities.Design
{
    /// <summary>
    /// Reads the annotation Studio keeps on an activity — the note in the bubble at the top right of a card.
    /// </summary>
    /// <remarks>
    /// Annotation text is stored in the workflow as the attached property <c>sap2010:Annotation.AnnotationText</c>,
    /// and it is not part of any contract UiPath documents or promises to keep. So this goes through
    /// reflection rather than binding to <c>ModelItem.Properties</c> at compile time: if a future Studio
    /// moves it, the note falls back to showing nothing rather than taking the designer down with it, and
    /// the package keeps loading.
    ///
    /// Two routes are tried, in the order they are most likely to work: the designer's own model, then the
    /// WPF attached-property store the model is built over.
    /// </remarks>
    internal static class AnnotationReader
    {
        /// <summary>The attached property's name, in both places it might be found under.</summary>
        private const string PropertyName = "AnnotationText";

        /// <summary>
        /// The annotation on the given model item, or null when there is none and when it cannot be found.
        /// </summary>
        /// <param name="modelItem">
        /// The designer's <c>ModelItem</c>, passed as <see cref="object"/> so this file compiles against
        /// either generation of the designer assemblies.
        /// </param>
        internal static string Read(object modelItem)
        {
            if (modelItem == null)
                return null;

            return FromModelProperties(modelItem) ?? FromAttachedProperties(modelItem);
        }

        /// <summary>
        /// Asks the model item for the property by name. This is the route that works in Studio today:
        /// annotations surface on the model alongside the activity's own properties.
        /// </summary>
        private static string FromModelProperties(object modelItem)
        {
            try
            {
                object properties = modelItem.GetType()
                    .GetProperty("Properties", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(modelItem, null);

                if (properties == null)
                    return null;

                // Find returns null for a property that is not there; the indexer throws, so Find it is.
                MethodInfo find = properties.GetType().GetMethod(
                    "Find", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);

                object property = find?.Invoke(properties, new object[] { PropertyName });

                if (property == null)
                    return null;

                object value = property.GetType()
                    .GetProperty("ComputedValue", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(property, null);

                return value as string;
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown note could not read the annotation from the model: " + exception);
                return null;
            }
        }

        /// <summary>
        /// Asks the WPF attachable-property store directly, over the activity instance the model wraps.
        /// The fallback for a host that keeps annotations off the model.
        /// </summary>
        private static string FromAttachedProperties(object modelItem)
        {
            try
            {
                object activity = modelItem.GetType()
                    .GetMethod("GetCurrentValue", BindingFlags.Public | BindingFlags.Instance)
                    ?.Invoke(modelItem, null);

                if (activity == null)
                    return null;

                Type annotation = Type.GetType(
                    "System.Activities.Presentation.Annotations.Annotation, System.Activities.Presentation");

                Type identifierType = Type.GetType(
                    "System.Windows.Markup.AttachableMemberIdentifier, System.Xaml");

                Type services = Type.GetType(
                    "System.Windows.Markup.AttachablePropertyServices, System.Xaml");

                if (annotation == null || identifierType == null || services == null)
                    return null;

                object identifier = Activator.CreateInstance(identifierType, annotation, PropertyName);

                // The non-generic overload: TryGetProperty(object, AttachableMemberIdentifier, out object).
                MethodInfo tryGet = services.GetMethod(
                    "TryGetProperty",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(object), identifierType, typeof(object).MakeByRefType() },
                    null);

                if (tryGet == null)
                    return null;

                var arguments = new[] { activity, identifier, null };

                return tryGet.Invoke(null, arguments) is bool found && found
                    ? arguments[2] as string
                    : null;
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown note could not read the annotation from the attached store: " + exception);
                return null;
            }
        }
    }
}
