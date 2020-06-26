using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Resources
{
    public class Resource<T>
    {
        public string Id { get; set; }
        public T Data { get; set; }
        public event Action<T, T> OnChanged;
        public void SetData(T data)
        {
            var oldData = Data;
            Data = data;
            OnChanged?.Invoke(oldData, data);
        }
    }
}
