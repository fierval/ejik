namespace AdvancedInspector
{
    /// <summary>
    /// This EditorWindow can recieve and send Toolbox inputs.
    /// </summary>
    public interface IModal
    {
        /// <summary>
        /// Called when the Toolbox shortcut is pressed.
        /// The implementation should call CreateToolbox if the condition are right.
        /// </summary>
        void ModalRequest(bool shift);

        /// <summary>
        /// Called when the associated toolbox is closed.
        /// Only called when Ok/Cancel or Enter/Escape is pressed.
        /// There's no reliable way of trapping the "click outside the popup" event.
        /// </summary>
        void ModalClosed(ModalWindow window);
    }
}