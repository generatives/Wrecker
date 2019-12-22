using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Clunker.SceneGraph;

namespace Clunker.Editor.Console
{
    public class StandardLibrary
    {
        public static GameObject FirstOrDefault(IEnumerable<GameObject> collection)
        {
            return collection.FirstOrDefault();
        }
    }
}
