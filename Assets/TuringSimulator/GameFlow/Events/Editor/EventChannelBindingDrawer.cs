using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using TuringSimulator.GameFlow.Events;

namespace TuringSimulator.GameFlow.Events.Editor
{
    [CustomPropertyDrawer(typeof(EventChannelActionListener.Binding))]
    public sealed class EventChannelBindingDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            root.Add(new PropertyField(property.FindPropertyRelative("Name")));
            var channelProperty = property.FindPropertyRelative("SourceChannel");
            root.Add(new PropertyField(channelProperty));
            root.Add(EventPayloadDocsInspector.CreateForChannelProperty(channelProperty));
            root.Add(new PropertyField(property.FindPropertyRelative("MatchProperty")));
            root.Add(new PropertyField(property.FindPropertyRelative("MatchValue")));
            root.Add(new PropertyField(property.FindPropertyRelative("OnMatched")));
            return root;
        }
    }
}
