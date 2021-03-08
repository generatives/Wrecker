using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Utilties
{
    public static class GraphicsExt
    {
        public static void DisposeWhenIdleIfNotNull(this GraphicsDevice graphicsDevice, IDisposable disposable)
        {
            if(disposable != null)
            {
                graphicsDevice.DisposeWhenIdle(disposable);
            }
        }
    }
}
