using System;

namespace AdvancedInspector
{
    internal delegate void DragEventHandler(object sender, DragEventArgs e);

    internal class DragEventArgs
    {
        private object target;

        /// <summary>
        /// Targeted object
        /// </summary>
        public object Target
        {
            get { return target; }
        }

        private bool inBetween;

        /// <summary>
        /// Is on top of the target
        /// </summary>
        public bool InBetween
        {
            get { return inBetween; }
        }

        private object[] dragged;

        /// <summary>
        /// Selecttion dragged
        /// </summary>
        public object[] Dragged
        {
            get { return dragged; }
        }

        public DragEventArgs(object target, bool inBetween, object[] dragged)
        {
            this.target = target;
            this.inBetween = inBetween;
            this.dragged = dragged;
        }
    }

    [Serializable]
    internal class DragDropWrapper
    {
        private string type;

        public string Type
        {
            get { return type; }
        }

        private object data;

        public object Data
        {
            get { return data; }
        }

        public DragDropWrapper(string type, object data)
        {
            this.type = type;
            this.data = data;
        }
    }
}