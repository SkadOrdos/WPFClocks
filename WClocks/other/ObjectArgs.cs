using System;

namespace WClocks
{
    public class ObjectArgs : ObjectArgs<object>
    {
        public ObjectArgs(object obj)
        {
            this.Object = obj;
        }
    }


    public class ObjectArgs<T> : EventArgs
    {
        public T Object { get; set; }

        public ObjectArgs(T obj)
        {
            this.Object = obj;
        }

        public ObjectArgs()
        { }
    }
}
