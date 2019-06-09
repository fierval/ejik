using System.Collections.Generic;

using UnityEditor;
using UnityEditor.VersionControl;
using UnityEditorInternal.VersionControl;
using UnityEngine;

namespace AdvancedInspector
{
    /// <summary>
    /// Similar to a standard PropertyInspector, but can be called from outside the scope of a Inspector.
    /// Useful to build your own EditorWindow with an integrated inspector.
    /// Call the method Draw with a Rect of the region to draw on.
    /// </summary>
    [CanEditMultipleObjects]
    public class ExternalEditor : InspectorEditor, IControl
    {
        private Vector2 scrollPosition;

        [SerializeField]
        private Separator separator = new Separator(null, null, 172, true, true);

        [SerializeField]
        private bool expandable = true;

        /// <summary>
        /// If false, the Inspector won't draw the expander and won't reserve space on the left of the labels.
        /// </summary>
        public override bool Expandable
        {
            get { return expandable; }
            set { expandable = value; }
        }

        /// <summary>
        /// Can the separator be dragged around?
        /// </summary>
        public bool DraggableSeparator
        {
            get { return separator.Draggable; }
            set { separator.Draggable = value; }
        }

        /// <summary>
        /// Where is the separator?
        /// In PerPixel, the value is in pixel from the top or the left.
        /// Otherwise, it's in %.
        /// </summary>
        public float DivisionSeparator
        {
            get { return separator.Division; }
            set { separator.Division = value; }
        }

        /// <summary>
        /// IControl implementation
        /// </summary>
        public event GenericEventHandler RequestRepaint;

        /// <summary>
        /// IControl implementation
        /// </summary>
        public bool Draw(Rect region)
        {
            GUILayout.BeginArea(region);

            bool redraw = false;
            if (Instances != null)
            {
                EditorGUILayout.BeginVertical();
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                redraw = AdvancedInspectorControl.Inspect(this, Fields, false, false, expandable, separator);

                if (GUI.changed)
                {
                    foreach (object instance in Instances)
                    {
                        if (instance is UnityEngine.Object)
                            EditorUtility.SetDirty(instance as UnityEngine.Object);

                        IDataChanged data = instance as IDataChanged;
                        if (data != null)
                            data.DataChanged();
                    }
                }

                EditorGUILayout.EndScrollView();

                GUILayout.FlexibleSpace();

                DrawProviderInfo();

                EditorGUILayout.EndVertical();
            }

            GUILayout.EndArea();

            return redraw;
        }

        private void Refresh()
        {
            if (RequestRepaint != null)
                RequestRepaint();
        }

        private void DrawProviderInfo()
        {
            bool locked = false;
            List<UnityEngine.Object> collection = new List<UnityEngine.Object>();
            for (int i = 0; i < Instances.Length; i++)
            {
                UnityEngine.Object obj = Instances[i] as UnityEngine.Object;
                if (obj != null)
                {
                    if (!AssetDatabase.IsOpenForEdit(obj, StatusQueryOptions.UseCachedIfPossible))
                        locked = true;

                    collection.Add(Instances[i] as UnityEngine.Object);
                }
            }

            if (collection.Count == 0)
                return;

            if (Provider.isActive && EditorSettings.externalVersionControl != ExternalVersionControl.Disabled && EditorSettings.externalVersionControl != ExternalVersionControl.AutoDetect && EditorSettings.externalVersionControl != ExternalVersionControl.Generic)
            {
                string assetPath = AssetDatabase.GetAssetPath(collection[0]);
                Asset assetByPath = Provider.GetAssetByPath(assetPath);
                if (assetByPath == null || (!assetByPath.path.StartsWith("Assets") && !assetByPath.path.StartsWith("ProjectSettings")))
                    return;

                EditorGUILayout.BeginHorizontal("preToolbar", GUILayout.Height(17));

                string text = StateToString(assetByPath.state);
                if (text != string.Empty && (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint))
                {
                    Texture2D texture2D = AssetDatabase.GetCachedIcon(assetPath) as Texture2D;
                    Rect rect = GUILayoutUtility.GetRect(16, 16);
                    if (texture2D != null)
                        GUI.Label(rect, texture2D);

                    Overlay.DrawOverlay(assetByPath, rect);
                    GUILayout.Label(text, "preToolbar2");
                }

                GUILayout.FlexibleSpace();

                if (locked)
                {
                    if (GUILayout.Button("Checkout", "preButton", GUILayout.Width(84)))
                    {
                        EditorPrefs.SetBool("vcssticky", true);
                        Task task = Provider.Checkout(collection.ToArray(), CheckoutMode.Both);
                        task.SetCompletionAction(CompletionAction.UpdatePendingWindow);
                        task.Wait();
                        base.Repaint();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private static bool IsState(Asset.States isThisState, Asset.States partOfThisState)
        {
            return (isThisState & partOfThisState) != Asset.States.None;
        }

        private static string StateToString(Asset.States state)
        {
            if (IsState(state, Asset.States.AddedLocal))
                return "Added Local";

            if (IsState(state, Asset.States.AddedRemote))
                return "Added Remote";

            if (IsState(state, Asset.States.CheckedOutLocal) && !IsState(state, Asset.States.LockedLocal))
                return "Checked Out Local";

            if (IsState(state, Asset.States.CheckedOutRemote) && !IsState(state, Asset.States.LockedRemote))
                return "Checked Out Remote";

            if (IsState(state, Asset.States.Conflicted))
                return "Conflicted";

            if (IsState(state, Asset.States.DeletedLocal))
                return "Deleted Local";

            if (IsState(state, Asset.States.DeletedRemote))
                return "Deleted Remote";

            if (IsState(state, Asset.States.Local))
                return "Local";

            if (IsState(state, Asset.States.LockedLocal))
                return "Locked Local";

            if (IsState(state, Asset.States.LockedRemote))
                return "Locked Remote";

            if (IsState(state, Asset.States.OutOfSync))
                return "Out Of Sync";

            return string.Empty;
        }
    }
}