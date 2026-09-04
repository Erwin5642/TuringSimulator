using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TuringSimulator.GameFlow.Events.Editor
{
    [CustomPropertyDrawer(typeof(AgentActionMapper.EventActionRule))]
    public sealed class EventActionRuleDrawer : PropertyDrawer
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
            root.Add(new PropertyField(property.FindPropertyRelative("Animation")));
            root.Add(new PropertyField(property.FindPropertyRelative("TextMode")));
            root.Add(new PropertyField(property.FindPropertyRelative("StaticText")));
            root.Add(new PropertyField(property.FindPropertyRelative("TextProperty")));
            root.Add(new PropertyField(property.FindPropertyRelative("SkipIfResolvedTextEmpty")));
            return root;
        }
    }
}
