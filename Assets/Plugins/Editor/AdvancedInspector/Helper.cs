using UnityEngine;

using Color = UnityEngine.Color;

namespace AdvancedInspector
{
    /// <summary>
    /// Collection of static helping method.
    /// </summary>
    public class Helper
    {
        /// <summary>
        /// Similar to GUI.DrawTexture, but draw a color instead.
        /// </summary>
        /// <param name="rect">Region to draw.</param>
        /// <param name="color">The color of the draw call.</param>
        public static void DrawColor(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = old;
        }
    }

}