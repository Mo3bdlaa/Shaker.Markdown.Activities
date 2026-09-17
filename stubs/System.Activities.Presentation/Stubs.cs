// Stand-ins for the real assembly Studio supplies. Signatures must match it exactly; see ../README.md.
using System.Activities.Presentation.Model;
using System.Windows.Controls;
using System.Windows.Media;

namespace System.Activities.Presentation
{
    /// <summary>Base of everything the designer draws on the canvas.</summary>
    public class WorkflowViewElement : ContentControl
    {
        /// <summary>The activity this element is drawn for.</summary>
        public ModelItem ModelItem { get; protected set; }
    }

    /// <summary>The card an activity is drawn as.</summary>
    public class ActivityDesigner : WorkflowViewElement
    {
        /// <summary>The glyph shown on the card.</summary>
        public DrawingBrush Icon { get; set; }

        /// <summary>Called when the designer learns which activity it belongs to.</summary>
        protected virtual void OnModelItemChanged(object newItem)
        {
        }
    }
}
