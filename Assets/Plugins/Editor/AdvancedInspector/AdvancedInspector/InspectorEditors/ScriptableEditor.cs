using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AdvancedInspector
{
    /// <summary>
    /// Scriptable Editor is the entry point for all ScriptableObject in Advanced Inspector.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ScriptableObject), true)]
    public class ScriptableEditor : InspectorEditor
    {
        private static List<object> tested = new List<object>();

        /// <summary>
        /// Should we check for the [AdvancedInspectorAttribute]?
        /// </summary>
        public override bool TestForDefaultInspector
        {
            get { return true; }
        }

        /// <summary>
        /// Unity's OnEnable.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            if (Instances.Length > 0 && Instances[0] is InspectorWrapper)
            {
                object[] wrapped = new object[Instances.Length];
                for (int i = 0; i < Instances.Length; i++)
                    wrapped[i] = ((InspectorWrapper)Instances[i]).Tag;

                Instances = wrapped;
            }
            else
            {
                foreach (object instance in Instances)
                    if (instance is ScriptableObject)
                        Validate(AssetDatabase.GetAssetPath((ScriptableObject)instance));
            }
        }

        private static void Validate(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            UnityEngine.Object[] components = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = components.Length - 1; i >= 0; i--)
            {
                ScriptableComponent component = components[i] as ScriptableComponent;
                if (component == null)
                    continue;

                tested.Clear();
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;

                if (component.Owner == null || !component.Owner || !Referenced(component.Owner, component))
                {
                    Debug.Log("Destroying " + components[i].ToString() + " because it is orphant.");
                    component.Erase();
                    continue;
                }
            }
        }

        private static bool Referenced(object owner, ScriptableComponent target)
        {
            if (owner == null || tested.Contains(owner))
                return false;
            else
                tested.Add(owner);

            Type type = owner.GetType();
            foreach (FieldInfo info in Clipboard.GetFields(type))
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(info.FieldType) && !info.FieldType.IsAssignableFrom(target.GetType()))
                    continue;

                object value = info.GetValue(owner);
                if (typeof(IList).IsAssignableFrom(info.FieldType))
                {
                    IList list = value as IList;
                    if (list != null)
                    {
                        foreach (object item in list)
                        {
                            if (ReferenceEquals(item, target))
                                return true;

                            if (item != null && item.GetType().IsClass && Referenced(item, target))
                                return true;
                        }
                    }
                }
                else if (typeof(IDictionary).IsAssignableFrom(info.FieldType))
                {
                    IDictionary dictionary = value as IDictionary;
                    if (dictionary != null)
                    {
                        foreach (DictionaryEntry entry in dictionary)
                        {
                            if (ReferenceEquals(entry.Key, target))
                                return true;

                            if (ReferenceEquals(entry.Value, target))
                                return true;

                            if (entry.Key != null && entry.Key.GetType().IsClass && Referenced(entry.Key, target))
                                return true;

                            if (entry.Value != null && entry.Value.GetType().IsClass && Referenced(entry.Value, target))
                                return true;
                        }
                    }
                }
                else if (info.FieldType.Namespace == "System" || info.FieldType.Namespace == "UnityEngine")
                    continue;
                else if (info.FieldType.IsAssignableFrom(target.GetType()))
                {
                    if (ReferenceEquals(value, target))
                        return true;
                }
                else if (info.FieldType.IsClass && Referenced(value, target))
                    return true;
            }

            return false;
        }
    }
}