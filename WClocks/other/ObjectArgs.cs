using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WClocks
{
    public class ObjectArgs : EventArgs
    {
        public object Object { get; set; }

        public ObjectArgs(object obj)
        {
            this.Object = obj;
        }

        public ObjectArgs()
        { }
    }
}
