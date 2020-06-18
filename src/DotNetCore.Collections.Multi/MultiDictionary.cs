using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.Collections.Multi
{
    // ReSharper disable InconsistentNaming
    public class MultiDictionary<K, V>
    {
        private readonly List<int> _hashcodeList;

        public MultiDictionary()
        {
            _hashcodeList = new List<int>();
        }
    }
}
