// Stand-ins for the real assembly Studio supplies. Signatures must match it exactly; see ../README.md.
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace System.Activities.Presentation.Model
{
    /// <summary>The designer's view of an activity instance.</summary>
    public abstract class ModelItem : INotifyPropertyChanged
    {
        /// <summary>Raised as the activity's properties are edited.</summary>
        public abstract event PropertyChangedEventHandler PropertyChanged;

        /// <summary>The type of the activity this item stands for.</summary>
        public abstract Type ItemType { get; }

        /// <summary>The activity's properties, as the designer sees them.</summary>
        public abstract ModelPropertyCollection Properties { get; }

        /// <summary>The activity instance itself.</summary>
        public abstract object GetCurrentValue();
    }

    /// <summary>One property of an activity, as the designer sees it.</summary>
    public abstract class ModelProperty
    {
        /// <summary>The property's name.</summary>
        public abstract string Name { get; }

        /// <summary>The value currently held, unwrapped from the model.</summary>
        public abstract object ComputedValue { get; set; }
    }

    /// <summary>The properties of one activity.</summary>
    public abstract class ModelPropertyCollection : IEnumerable<ModelProperty>
    {
        /// <summary>Gets a property by name, throwing when there is no such property.</summary>
        public abstract ModelProperty this[string propertyName] { get; }

        /// <summary>Gets a property by name, or null when there is no such property.</summary>
        public abstract ModelProperty Find(string propertyName);

        /// <inheritdoc />
        public abstract IEnumerator<ModelProperty> GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

namespace System.Activities.Presentation.Metadata
{
    /// <summary>A built set of attributes to merge into the designer's metadata.</summary>
    public class AttributeTable
    {
        /// <summary>The types the table carries attributes for.</summary>
        public IEnumerable<Type> AttributedTypes => throw new NotImplementedException();
    }

    /// <summary>Collects attributes to attach to types and their members at design time.</summary>
    public class AttributeTableBuilder
    {
        /// <summary>Attaches attributes to a type.</summary>
        public void AddCustomAttributes(Type type, params Attribute[] attributes) => throw new NotImplementedException();

        /// <summary>Attaches attributes to one property of a type.</summary>
        public void AddCustomAttributes(Type type, string propertyName, params Attribute[] attributes) => throw new NotImplementedException();

        /// <summary>Builds the table.</summary>
        public AttributeTable CreateTable() => throw new NotImplementedException();
    }

    /// <summary>Where built attribute tables are handed to the designer.</summary>
    public static class MetadataStore
    {
        /// <summary>Merges a table into the designer's metadata.</summary>
        public static void AddAttributeTable(AttributeTable table) => throw new NotImplementedException();
    }

    /// <summary>Implemented by an assembly that wants to register designer metadata.</summary>
    public interface IRegisterMetadata
    {
        /// <summary>Called once when the package is loaded.</summary>
        void Register();
    }
}
