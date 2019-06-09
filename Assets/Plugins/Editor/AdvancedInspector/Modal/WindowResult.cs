namespace AdvancedInspector
{
    /// <summary>
    /// Result returned by a Modal Window
    /// </summary>
    public enum WindowResult
    {
        /// <summary>
        /// No result, no action taken
        /// </summary>
        None,
        /// <summary>
        /// Ok or Enter
        /// </summary>
        Ok,
        /// <summary>
        /// Cancel or Escape
        /// </summary>
        Cancel,
        /// <summary>
        /// An error occured
        /// </summary>
        Invalid,
        /// <summary>
        /// User clicked outside the modal
        /// </summary>
        LostFocus
    }
}