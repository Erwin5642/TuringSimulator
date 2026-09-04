using TuringSimulator.GameFlow.Events;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TuringSimulator.GameFlow.Events.Editor
{
    [CustomEditor(typeof(EventChannelSO), true)]
    [CanEditMultipleObjects]
    public sealed class EventChannelSOEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            if (targets.Length == 1)
                root.Add(EventPayloadDocsInspector.CreateForAsset(target));
            else
                root.Add(new HelpBox("Select a single channel to see payload docs.", HelpBoxMessageType.Info));

            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }
    }
}
