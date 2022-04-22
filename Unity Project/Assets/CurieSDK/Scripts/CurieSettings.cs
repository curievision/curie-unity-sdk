using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurieSDK
{
    [Serializable]
    public class CurieSettings
    {
        /// <summary>
        /// Not supported on WebGL
        /// </summary>
        public bool CacheModels = false;

        public bool ResetModelsToCenter = true;
        public bool DisableObjectsOnSpawn = false;
        public bool VerboseLogging = false;
    }
}
