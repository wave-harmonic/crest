// Taken from:
// https://github.com/vlab22/unityevent-reordering/blob/da069c1bb63bff9111bacb77b0b62ee5b5865fdc/Assets/Editor/ReorderingUnityEventDrawer.cs

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
