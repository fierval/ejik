using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace AdvancedInspector
{
    [InitializeOnLoad]
    internal class InspectorPersist
    {
        private static InspectorPersist instance;

        public static InspectorPersist Instance
        {
            get
            {
                if (instance == null)
                    instance = new InspectorPersist();

                return instance;
            }
        }

        [SerializeField]
        private List<InspectorReference> references = new List<InspectorReference>();

        static InspectorPersist()
        {
#if UNITY_2017_1
            EditorApplication.playmodeStateChanged += PlaymodeChanged;
#else
            EditorApplication.playModeStateChanged += PlaymodeChanged;
#endif
        }

        private InspectorPersist() { }

#if UNITY_2017_1
        private static void PlaymodeChanged()
#else
        private static void PlaymodeChanged(PlayModeStateChange changed)
#endif
        {
            if (EditorApplication.isPlaying)
                return;

            for (int i = 0; i < Instance.references.Count; i++)
                Instance.references[i].Save();

            Instance.references.Clear();
        }

        public static bool Contains(InspectorField field)
        {
            InspectorReference reference = new InspectorReference(field);
            foreach (InspectorReference child in Instance.references)
                if (child.Equals(reference))
                    return true;

            return false;
        }

        public static void AddField(InspectorField field)
        {
            if (Contains(field))
                return;

            Instance.references.Add(new InspectorReference(field));
        }

        public static void RemoveField(InspectorField field)
        {
            InspectorReference reference = new InspectorReference(field);

            for (int i = Instance.references.Count - 1; i >= 0; i--)
                if (reference.Equals(Instance.references[i]))
                    Instance.references.RemoveAt(i);
        }
    }
}
