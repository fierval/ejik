using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEngine;

namespace AdvancedInspector
{
    /// <summary>
    /// Behaviour Editor is the entry point for all MonoBehaviour in Advanced Inspector.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class BehaviourEditor : InspectorEditor
    {
        private static List<object> tested = new List<object>();

        private static Type audioUtil;
        private static MethodInfo haveAudioCallback;
        private static MethodInfo getCustomFilterChannelCount;
        private static MethodInfo getCustomFilterProcessTime;
        private static MethodInfo getCustomFilterMaxOut;

        private static GUIStyle progressBarBack;

        private static Texture2D horizontalVUTexture;

        private static Texture2D HorizontalVUTexture
        {
            get
            {
                if (horizontalVUTexture == null)
                    horizontalVUTexture = typeof(EditorGUIUtility).GetMethod("LoadIcon", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { "VUMeterTextureHorizontal" }) as Texture2D;

                return horizontalVUTexture;
            }
        }

        private SmoothingData[] dataOut;

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

            foreach (object instance in Instances)
                if (instance is Component)
                    Validate(((Component)instance).gameObject);

            if (audioUtil == null)
                audioUtil = TypeUtility.GetTypeByName("AudioUtil");

            if (haveAudioCallback == null)
                haveAudioCallback = audioUtil.GetMethod("HaveAudioCallback", BindingFlags.Static | BindingFlags.Public);

            if (getCustomFilterChannelCount == null)
                getCustomFilterChannelCount = audioUtil.GetMethod("GetCustomFilterChannelCount", BindingFlags.Static | BindingFlags.Public);

            if (getCustomFilterProcessTime == null)
                getCustomFilterProcessTime = audioUtil.GetMethod("GetCustomFilterProcessTime", BindingFlags.Static | BindingFlags.Public);

            if (getCustomFilterMaxOut == null)
                getCustomFilterMaxOut = audioUtil.GetMethod("GetCustomFilterMaxOut", BindingFlags.Static | BindingFlags.Public);
        }

        /// <summary>
        /// Override to display Audio VU
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (progressBarBack == null)
                progressBarBack = GetStyle("ProgressBarBack");

            base.OnInspectorGUI();

            if (getCustomFilterChannelCount == null || haveAudioCallback == null ||
                getCustomFilterMaxOut == null || getCustomFilterProcessTime == null)
                return;

            object[] param = new object[] { target as MonoBehaviour };
            int channelCount = (int)getCustomFilterChannelCount.Invoke(null, param);
            if ((bool)haveAudioCallback.Invoke(null, param) && channelCount > 0)
                DrawAudioFilterGUI(channelCount, param);
        }

        private static void Validate(GameObject go)
        {
            ComponentMonoBehaviour[] components = go.GetComponents<ComponentMonoBehaviour>();
            for (int i = components.Length - 1; i >= 0; i--)
            {
                ComponentMonoBehaviour component = components[i];
                if (component == null)
                    return;

                tested.Clear();
                component.hideFlags = HideFlags.HideInInspector;

                if (component.Owner == null || !component.Owner || !Referenced(component.Owner, component))
                {
                    Debug.Log("Destroying " + component.ToString() + " because it is orphant.");
                    component.Erase();
                    continue;
                }
            }
        }

        private static bool Referenced(object owner, ComponentMonoBehaviour target)
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

        private GUIStyle GetStyle(string styleName)
        {
            GUIStyle gUIStyle = GUI.skin.FindStyle(styleName);
            if (gUIStyle == null)
                gUIStyle = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);

            return gUIStyle;
        }

        private void DrawAudioFilterGUI(int channelCount, object[] param)
        {
            if (channelCount > 0)
            {
                if (dataOut == null)
                    dataOut = new SmoothingData[channelCount];

                double num = (double)(int)getCustomFilterProcessTime.Invoke(null, param) / 1000000.0;
                float num2 = (float)num / ((float)AudioSettings.outputSampleRate / 1024f / (float)channelCount);
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                GUILayout.Space(13f);
                GUILayout.BeginVertical(new GUILayoutOption[0]);
                EditorGUILayout.Space();
                for (int i = 0; i < channelCount; i++)
                {
                    float maxOut = (float)getCustomFilterMaxOut.Invoke(null, new object[] { param[0], i });
                    VUMeterHorizontal(maxOut, ref dataOut[i], GUILayout.MinWidth(50f), GUILayout.Height(5f));
                }
                GUILayout.EndVertical();
                Color color = GUI.color;
                GUI.color = new Color(num2, 1f - num2, 0f, 1f);
                GUILayout.Box(string.Format("{0:00.00}ms", num), GUILayout.MinWidth(40f), GUILayout.Height(20f));
                GUI.color = color;
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();

                Repaint();
            }
        }

        private static void VUMeterHorizontal(float value, ref SmoothingData data, params GUILayoutOption[] options)
        {
            Rect position = EditorGUILayout.GetControlRect(false, 16f, EditorStyles.numberField, options);
            HorizontalMeter(position, value, ref data, HorizontalVUTexture, Color.grey);
        }

        private static void HorizontalMeter(Rect position, float value, ref SmoothingData data, Texture2D foregroundTexture, Color peakColor)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            float value2;
            float peak;

            SmoothVUMeterData(ref value, ref data, out value2, out peak);
            HorizontalMeter(position, value2, peak, foregroundTexture, peakColor);
        }

        private static void SmoothVUMeterData(ref float value, ref SmoothingData data, out float renderValue, out float renderPeak)
        {
            if (value <= data.lastValue)
            {
                value = Mathf.Lerp(data.lastValue, value, Time.smoothDeltaTime * 7f);
            }
            else
            {
                value = Mathf.Lerp(value, data.lastValue, Time.smoothDeltaTime * 2f);
                data.peakValue = value;
                data.peakValueTime = Time.realtimeSinceStartup;
            }

            if (value > 1.11111116f)
                value = 1.11111116f;

            if (data.peakValue > 1.11111116f)
                data.peakValue = 1.11111116f;

            renderValue = value * 0.9f;
            renderPeak = data.peakValue * 0.9f;
            data.lastValue = value;
        }

        private static void HorizontalMeter(Rect position, float value, float peak, Texture2D foregroundTexture, Color peakColor)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            progressBarBack.Draw(position, false, false, false, false);

            float posValue = position.width * value - 2f;
            if (posValue < 2f)
                posValue = 2f;

            Rect position2 = new Rect(position.x + 1f, position.y + 1f, posValue, position.height - 2f);
            Rect texCoords = new Rect(0f, 0f, value, 1f);

            Color color = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, (!GUI.enabled) ? 0.5f : 1f);
            GUI.DrawTextureWithTexCoords(position2, foregroundTexture, texCoords);

            float posPeak = position.width * peak - 2f;
            if (posPeak < 2f)
                posPeak = 2f;

            position2 = new Rect(position.x + posPeak, position.y + 1f, 1f, position.height - 2f);

            GUI.color = peakColor;
            GUI.DrawTexture(position2, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = color;
        }

        private struct SmoothingData
        {
            public float lastValue;
            public float peakValue;
            public float peakValueTime;
        }
    }
}