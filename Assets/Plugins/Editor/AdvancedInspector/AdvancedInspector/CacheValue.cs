namespace AdvancedInspector
{
    internal class CacheValue<T>
    {
        private T value;

        public T Value
        {
            get { return value; }
        }

        private bool cached = false;

        public bool Cached
        {
            get { return cached; }
        }

        public void Cache(T value)
        {
            this.value = value;
            cached = true;
        }

        public void Clear()
        {
            value = default(T);
            cached = false;
        }
    }
}
