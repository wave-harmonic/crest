// Taken from:
// https://github.com/vlab22/unityevent-reordering/blob/da069c1bb63bff9111bacb77b0b62ee5b5865fdc/Assets/Editor/ReorderingUnityEventDrawer.cs
// https://answers.unity.com/questions/1398221/how-to-re-arrange-button-onclick-event-in-unity.html?#answer-1846873
// https://forum.unity.com/threads/reorder-unity-events-in-the-inspector.350712/#post-7302118

// Event execution order is random. Only use for cleanliness.

namespace Crest.Development
{
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine.Events;

    [CustomPropertyDrawer(typeof(UnityEventBase), true)]
    public class ReorderableUnityEventDrawer : UnityEventDrawer
    {
        protected override void SetupReorderableList(ReorderableList list)
        {
            base.SetupReorderableList(list);

            list.draggable = true;
        }
    }
}
