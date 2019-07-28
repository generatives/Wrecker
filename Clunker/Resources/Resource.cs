using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Resources
{
    public class Resource<T>
    {
        public string Id { get; set; }
        public T Data { get; set; }
    }
}
