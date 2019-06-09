using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace AdvancedInspector
{
    /// <summary>
    /// A copy/paste buffer.
    /// Does not use the System buffer, as it's limited to a string and Unity does not provide a uniformed string serialization.
    /// </summary>
    internal class Clipboard
    {
        private static Dictionary<object, object> pairs = new Dictionary<object, object>();

        private static object buffer;

        /// <summary>
        /// In case of any normal object, returns deep copy of that object.
        /// In case of a Component, returns the original so you can apply it on a GameObject yourself (sorry)
        /// In case of a UnityEngine.Object, create a new one and perforce a Serialization Copy.
        /// </summary>
        public static object Data
        {
            get { return buffer; }
            set { buffer = value; }
        }

        /// <summary>
        /// Create an instance of type
        /// Works with values, list, array, scriptableObject, string
        /// Components are ignored.
        /// 
        /// The owner MonoBeahviour is used in case of the creation of a ComponentMonoBehaviour for its internal binding.
        /// </summary>
        public static object CreateInstance(Type type, UnityEngine.Object owner = null)
        {
            if (typeof(GameObject).IsAssignableFrom(type))
                return null;
            else if (typeof(ComponentMonoBehaviour).IsAssignableFrom(type) && owner != null && owner is MonoBehaviour)
            {
                ComponentMonoBehaviour component = ((MonoBehaviour)owner).gameObject.AddComponent(type) as ComponentMonoBehaviour;
                component.Owner = owner as MonoBehaviour;
                return component;
            }
            else if (typeof(ScriptableComponent).IsAssignableFrom(type) && owner != null && owner is ScriptableObject)
            {
                ScriptableComponent component = ScriptableObject.CreateInstance(type) as ScriptableComponent;
                component.Owner = owner as ScriptableObject;

                string path = AssetDatabase.GetAssetPath(owner);
                if (string.IsNullOrEmpty(path))
                    return null;

                AssetDatabase.AddObjectToAsset(component, path);
                return component;
            }
            else if (typeof(Component).IsAssignableFrom(type))
            {
                if (owner is GameObject)
                    return ((GameObject)owner).AddComponent(type);

                return null;
            }
            else if (typeof(ScriptableObject).IsAssignableFrom(type))
                return null;
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return null;
            else if (type.IsArray)
                return Array.CreateInstance(type.GetElementType(), 0);
            else if (type == typeof(string))
                return "";
            else
                return Activator.CreateInstance(type, true);
        }

        /// <summary>
        /// Perform a deep copy if an object.
        /// Does not perform copies of Components or GameObject
        /// Values are returned as is (ex.: 14)
        /// strings are cloned.
        /// List/Array are enumerated and copied.
        /// Everything else is deep copied by fields.
        /// </summary>
        public static object Copy(object data, UnityEngine.Object owner = null)
        {
            pairs = new Dictionary<object, object>();
            return DeepCopy(data, owner);
        }

        private static object DeepCopy(object data, UnityEngine.Object owner)
        {
            if (!pairs.ContainsKey(owner))
                pairs.Add(owner, owner);

            if (data == null || (data is UnityEngine.Object && !((UnityEngine.Object)data)))
                return null;

            if (pairs.ContainsKey(data))
                return pairs[data];

            Type type = data.GetType();
            object copy = null;

            if (typeof(ComponentMonoBehaviour).IsAssignableFrom(type))
            {
                MonoBehaviour parent = owner as MonoBehaviour;
                if (parent == null)
                    return data;

                ComponentMonoBehaviour component = Clipboard.CreateInstance(type, owner) as ComponentMonoBehaviour;
                pairs.Add(data, component);

                IterateObject(data, component, parent);
                component.Owner = parent;

                return component;
            }
            else if (typeof(Component).IsAssignableFrom(type) || typeof(GameObject).IsInstanceOfType(type) || type.Namespace == "System")
                return data;
            else if (data is AnimationCurve)
            {
                AnimationCurve original = data as AnimationCurve;
                AnimationCurve animationCopy = new AnimationCurve();
                animationCopy.keys = original.keys;
                animationCopy.preWrapMode = original.preWrapMode;
                animationCopy.postWrapMode = original.postWrapMode;
                return animationCopy;
            }
            else if (data is string)
            {
                return ((string)data).Clone();
            }
            else if (type.IsArray)
            {
                copy = Array.CreateInstance(type.GetElementType(), ((Array)data).Length);
                for (int i = 0; i < ((Array)data).Length; i++)
                    ((Array)copy).SetValue(DeepCopy(((Array)data).GetValue(i), owner), i);
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                copy = CreateInstance(type);
                foreach (object o in ((IList)data))
                    ((IList)copy).Add(DeepCopy(o, owner));
            }
            else if (typeof(IDictionary).IsAssignableFrom(type))
            {
                copy = CreateInstance(type);
                foreach (DictionaryEntry entry in ((IDictionary)data))
                    ((IDictionary)copy).Add(DeepCopy(entry.Key, owner), DeepCopy(entry.Value, owner));
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                copy = (UnityEngine.Object)CreateInstance(type);
                if (copy == null)
                    return data;

                EditorUtility.CopySerialized((UnityEngine.Object)data, (UnityEngine.Object)copy);
            }
            else
            {
                copy = CreateInstance(type);
                pairs.Add(data, copy);

                IterateObject(data, copy, owner);
            }

            return copy;
        }

        private static void IterateObject(object original, object copy, UnityEngine.Object owner)
        {
            Type type = copy.GetType();
            foreach (FieldInfo info in GetFields(type))
            {
                if (info.IsInitOnly || info.IsLiteral)
                    continue;

                object value = info.GetValue(original);
                if (value == null || ReferenceEquals(value, owner))
                    continue;

                if (pairs.ContainsKey(value))
                {
                    info.SetValue(copy, pairs[value]);
                    continue;
                }

                Type valueType = value.GetType();

                if (typeof(ComponentMonoBehaviour).IsAssignableFrom(valueType))
                {
                    info.SetValue(copy, DeepCopy(value, owner));
                }
                else if (typeof(UnityEngine.Object).IsAssignableFrom(valueType) || (valueType.Namespace == "System"))
                {
                    info.SetValue(copy, value);
                }
                else
                {
                    info.SetValue(copy, DeepCopy(value, owner));
                }
            }
        }

        /// <summary>
        /// Type of the current data in the clipboard.
        /// </summary>
        public static Type Type
        {
            get
            {
                if (buffer == null)
                    return null;

                return buffer.GetType();
            }
        }

        /// <summary>
        /// Test if the current clipboard data can be converted into a specific type.
        /// Ex.: int to string
        /// </summary>
        public static bool TryConvertion(Type type, out object value)
        {
            if (type.IsAssignableFrom(Type))
            {
                value = Data;
                return true;
            }

            try
            {
                value = Convert.ChangeType(Data, type);
                return true;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Test if the current clipboard data can be converted into a specific type.
        /// Ex.: int to string
        /// </summary>
        public static bool TryConvertion<T>(out T value)
        {
            try
            {
                value = (T)Convert.ChangeType(Data, typeof(T));
                return true;
            }
            catch (Exception)
            {
                value = default(T);
                return false;
            }
        }

        /// <summary>
        /// Return true if an object is convertable to a type.
        /// </summary>
        public static bool CanConvert(Type type, object value)
        {
            if (value == null)
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                    return true;
                else
                    return false;
            }

            Type valueType = value.GetType();
            if (valueType.IsValueType && typeof(UnityEngine.Object).IsAssignableFrom(valueType))
                return false;

            if (type.IsAssignableFrom(valueType))
                return true;

            try
            {
                value = Convert.ChangeType(value, type);
                return true;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        public static List<FieldInfo> GetFields(Type type)
        {
            List<FieldInfo> infos = new List<FieldInfo>();

            while (type != null)
            {
                infos.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));

                bool ignoreBase = type.GetCustomAttributes(typeof(IgnoreBase), true).Length > 0;
                if (!ignoreBase && type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(UnityEngine.Object))
                    type = type.BaseType;
                else
                    break;
            }

            return infos;
        }

        public static List<MemberInfo> GetMembers(Type type)
        {
            List<MemberInfo> infos = new List<MemberInfo>();

            while (type != null)
            {
                infos.AddRange(type.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));

                bool ignoreBase = type.GetCustomAttributes(typeof(IgnoreBase), true).Length > 0;
                if (!ignoreBase && type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(UnityEngine.Object))
                    type = type.BaseType;
                else
                    break;
            }

            return infos;
        }
    }
}