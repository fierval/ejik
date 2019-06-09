using UnityEngine;
using UnityEditor;
using System;

namespace AdvancedInspector
{
    /// <summary>
    /// A wrapper recongnized by the internal inspector so that non-UnityEngine.Object can be inspected.
    /// </summary>
    [Serializable]
    public class InspectorWrapper : ScriptableObject
    {
        [SerializeField]
        private object tag;

        /// <summary>
        /// Object to Inspector
        /// </summary>
        public object Tag
        {
            get { return tag; }
            set { tag = value; }
        }

        /// <summary>
        /// Force Unity's "Selection" to use a non-unity object.
        /// </summary>
        public static void Select(object tag)
        {
            InspectorWrapper wrapper = CreateInstance<InspectorWrapper>();
            wrapper.tag = tag;
            Selection.activeObject = wrapper;
        }

        /// <summary>
        /// Is this object selected by Unity's Selection?
        /// </summary>
        public static bool IsSelected(object tag)
        {
            InspectorWrapper wrapper = Selection.activeObject as InspectorWrapper;

            if (wrapper == null)
                return false;

            return wrapper.tag == tag;
        }

        /// <summary>
        /// Get currenty Unity's Selection 
        /// </summary>
        public static T GetSelection<T>()
        {
            InspectorWrapper wrapper = Selection.activeObject as InspectorWrapper;

            if (wrapper == null || !typeof(T).IsAssignableFrom(wrapper.tag.GetType()))
                return default(T);

            return (T)wrapper.tag;
        }
    }
}