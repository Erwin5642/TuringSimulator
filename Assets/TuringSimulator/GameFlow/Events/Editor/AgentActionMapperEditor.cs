using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TuringSimulator.GameFlow.Events.Editor
{
    [CustomEditor(typeof(AgentActionMapper))]
    [CanEditMultipleObjects]
    public sealed class AgentActionMapperEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }
    }
}
