using System;

namespace Kamban.Export.Options
{
    [Flags]
    public enum ScaleFitting
    {
        None = 0,
        Vertical = 1,
        Horizontal = 2,
        BothDirections = 3
    }
}