using TuringSimulator.GameFlow.Events;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TuringSimulator.GameFlow.Events.Editor
{
    [CustomEditor(typeof(EventChannelActionListener))]
    [CanEditMultipleObjects]
    public sealed class EventChannelActionListenerEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }
    }
}
