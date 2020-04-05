using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.SceneGraph.SceneSystemInterfaces
{
    public interface ISystemEventProcessor
    {
        void SystemStarted();
        void SystemStopped();
    }
}
