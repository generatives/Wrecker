using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Editor.Utilities.PropertyEditor
{
    public interface IPropertyEditor
    {
        (bool, object) DrawEditor(string label, object value);
    }
}
