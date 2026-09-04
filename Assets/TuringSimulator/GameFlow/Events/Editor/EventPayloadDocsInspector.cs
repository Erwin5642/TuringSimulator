using TuringSimulator.GameFlow.Events;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TuringSimulator.GameFlow.Events.Editor
{
    internal static class EventPayloadDocsInspector
    {
        public static VisualElement CreateForAsset(UnityEngine.Object channelAsset)
        {
            var box = CreateBox();
            Apply(box, channelAsset);
            return box;
        }

        public static VisualElement CreateForChannelProperty(SerializedProperty channelProperty)
        {
            var box = CreateBox();
            void Refresh(SerializedProperty _) => Apply(box, channelProperty.objectReferenceValue);
            box.TrackPropertyValue(channelProperty, Refresh);
            Refresh(channelProperty);
            return box;
        }

        static HelpBox CreateBox()
        {
            var box = new HelpBox(string.Empty, HelpBoxMessageType.Info);
            box.style.marginTop = 4;
            box.style.marginBottom = 6;
            box.style.whiteSpace = WhiteSpace.PreWrap;
            return box;
        }

        static void Apply(HelpBox box, UnityEngine.Object channelAsset)
        {
            if (channelAsset == null)
            {
                box.style.display = DisplayStyle.None;
                return;
            }

            if (!EventPayloadSchema.TryFormatInspectorDocs(channelAsset, out var docs))
            {
                box.style.display = DisplayStyle.None;
                return;
            }

            box.style.display = DisplayStyle.Flex;
            box.messageType = EventPayloadSchema.TryGetPayloadType(channelAsset.GetType()) == null
                ? HelpBoxMessageType.Warning
                : HelpBoxMessageType.Info;
            box.text = docs;
        }
    }
}
