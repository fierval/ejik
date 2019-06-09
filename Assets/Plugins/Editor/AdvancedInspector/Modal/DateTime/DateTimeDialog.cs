using System;

using UnityEngine;
using UnityEditor;

namespace AdvancedInspector
{
    /// <summary>
    /// The DateTime dialog is a date picker that is day/week accurate.
    /// </summary>
    public class DateTimeDialog : ModalWindow
    {
        /// <summary>
        /// Height of dialog
        /// </summary>
        public const float HEIGHT = 258;
        /// <summary>
        /// Width of dialog
        /// </summary>
        public const float WIDTH = 268;

        private DateTime time;

        /// <summary>
        /// Modified DateTime if Result is Ok
        /// </summary>
        public DateTime Time
        {
            get { return time; }
        }

        private string[] months = new string[12];

        private static Texture scrollLeft;
        private static Texture scrollLeftPro;

        internal static Texture ScrollLeft
        {
            get
            {
                if (!EditorGUIUtility.isProSkin)
                {
                    if (scrollLeftPro == null)
                        scrollLeftPro = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "ScrollLeft.png");

                    return scrollLeftPro;
                }
                else
                {
                    if (scrollLeft == null)
                        scrollLeft = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "ScrollLeft_Light.png");

                    return scrollLeft;
                }
            }
        }

        private static Texture scrollRight;
        private static Texture scrollRightPro;

        internal static Texture ScrollRight
        {
            get
            {
                if (!EditorGUIUtility.isProSkin)
                {
                    if (scrollRightPro == null)
                        scrollRightPro = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "ScrollRight.png");

                    return scrollRightPro;
                }
                else
                {
                    if (scrollRight == null)
                        scrollRight = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "ScrollRight_Light.png");

                    return scrollRight;
                }
            }
        }

        /// <summary>
        /// Create this dialog.
        /// </summary>
        public static DateTimeDialog Create(IModal owner, DateTime time, Vector2 position)
        {
            DateTimeDialog dialog = DateTimeDialog.CreateInstance<DateTimeDialog>();

            float halfWidth = WIDTH / 2;

            float x = position.x - halfWidth;
            float y = position.y;

            Rect rect = new Rect(x, y, 0, 0);

            dialog.owner = owner;
            dialog.time = time;
            dialog.position = rect;
            dialog.titleContent.text = "Date Time";

            DateTime month = new DateTime(1, 1, 1);
            for (int i = 0; i < 12; i++)
            {
                dialog.months[i] = month.ToString("MMMM");
                month = month.AddMonths(1);
            }

            dialog.ShowAsDropDown(rect, new Vector2(WIDTH, HEIGHT));

            return dialog;
        }

        /// <summary>
        /// Draw the dialog region.
        /// </summary>
        protected override void Draw(Rect region)
        {
            TextAnchor anchor = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;

            GUILayout.BeginArea(region);

            // Year
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(ScrollLeft, EditorStyles.toolbarButton))
                time = time.AddYears(-1);

            int year = EditorGUILayout.IntField(time.Year, EditorStyles.toolbarTextField, GUILayout.Width(100));
            if (year != time.Year)
                time = new DateTime(year, time.Month, time.Day, time.Hour, time.Minute, time.Second);

            if (GUILayout.Button(ScrollRight, EditorStyles.toolbarButton))
                time = time.AddYears(1);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Month
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(ScrollLeft, EditorStyles.toolbarButton))
                time = time.AddMonths(-1);

            int dayCount = DateTime.DaysInMonth(time.Year, time.Month);
            int selected = EditorGUILayout.Popup(time.Month - 1, months, EditorStyles.toolbarPopup, GUILayout.Width(108)) + 1;
            dayCount = DateTime.DaysInMonth(time.Year, selected);
            if (selected != time.Month)
                time = new DateTime(time.Year, selected, Mathf.Clamp(time.Day, 1, dayCount), time.Hour, time.Minute, time.Second);

            if (GUILayout.Button(ScrollRight, EditorStyles.toolbarButton))
                time = time.AddMonths(1);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Day
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();

            GUILayout.Label("S", GUILayout.Width(32));
            GUILayout.Label("M", GUILayout.Width(32));
            GUILayout.Label("T", GUILayout.Width(32));
            GUILayout.Label("W", GUILayout.Width(32));
            GUILayout.Label("T", GUILayout.Width(32));
            GUILayout.Label("F", GUILayout.Width(32));
            GUILayout.Label("S", GUILayout.Width(32));
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            DayOfWeek day = new DateTime(time.Year, time.Month, 1).DayOfWeek;
            int dayNumber = 1;
            int empty = (int)day;
            int row = 0;
            while (row < 6)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                for (int d = 0; d < 7; d++)
                {
                    if (empty > 0)
                    {
                        GUILayout.Label("", GUILayout.Width(32));
                        empty--;
                    }
                    else if (dayCount <= 0)
                    {
                        GUILayout.Label("", GUILayout.Width(32));
                    }
                    else
                    {
                        if (GUILayout.Toggle((dayNumber == time.Day), dayNumber.ToString(), EditorStyles.miniButton, GUILayout.Width(32)))
                            time = new DateTime(time.Year, time.Month, dayNumber, time.Hour, time.Minute, time.Second);

                        dayNumber++;
                        dayCount--;
                    }
                }
                row++;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            GUI.skin.label.alignment = TextAnchor.MiddleRight;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Hour", GUILayout.Width(48));
            int hour = EditorGUILayout.IntSlider("", time.Hour, 0, 23);
            if (hour != time.Hour)
                time = new DateTime(time.Year, time.Month, time.Day, hour, time.Minute, time.Second);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Minute", GUILayout.Width(48));
            int minute = EditorGUILayout.IntSlider("", time.Minute, 0, 59);
            if (minute != time.Minute)
                time = new DateTime(time.Year, time.Month, time.Day, time.Hour, minute, time.Second);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Second", GUILayout.Width(48));
            int second = EditorGUILayout.IntSlider("", time.Second, 0, 59);
            if (second != time.Second)
                time = new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, second);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ok"))
                Ok();

            if (GUILayout.Button("Cancel"))
                Cancel();
            EditorGUILayout.EndHorizontal();

            GUILayout.EndArea();

            GUI.skin.label.alignment = anchor;
        }
    }
}
