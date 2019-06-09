using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AdvancedInspector
{
    [InitializeOnLoad]
    internal class InspectorCoroutine
    {
        private static List<IEnumerator> coroutines = new List<IEnumerator>();

        private static Dictionary<WaitForSeconds, DateTime> timers = new Dictionary<WaitForSeconds, DateTime>();

        private static FieldInfo waitForSecondTime;

        static InspectorCoroutine()
        {
            EditorApplication.update += Update;

            waitForSecondTime = typeof(WaitForSeconds).GetField("m_Seconds", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static void StartCoroutine(IEnumerator routine)
        {
            routine.MoveNext();
            coroutines.Add(routine);
        }

        private static void Update()
        {
            for (int i = coroutines.Count - 1; i >= 0; i--)
            {
                IEnumerator enumerator = coroutines[i];
                WaitForSeconds waitForSeconds = enumerator.Current as WaitForSeconds;
                if (waitForSeconds != null)
                {
                    if (timers.ContainsKey(waitForSeconds))
                    {
                        float time = (float)waitForSecondTime.GetValue(enumerator.Current);
                        TimeSpan span = DateTime.Now - timers[waitForSeconds];
                        if (span.TotalSeconds < time)
                            continue;

                        timers.Remove(waitForSeconds);
                    }
                    else
                    {
                        timers.Add(waitForSeconds, DateTime.Now);
                        continue;
                    }
                }

                if (!enumerator.MoveNext())
                    coroutines.RemoveAt(i);
            }
        }
    }
}
