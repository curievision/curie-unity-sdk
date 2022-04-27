using System;

namespace CurieSDK
{
    [Serializable]
    public class CurieSettings
    {
        /// <summary>
        /// Caching is not supported on WebGL
        /// </summary>
        public bool CacheModels = false;

        /// <summary>
        /// Reset models to Vector3.Zero when Instantiated
        /// </summary>
        public bool ResetModelsToCenter = true;

        /// <summary>
        /// Set's the gameobject's Active property to FALSE when Instantiated
        /// </summary>
        public bool DisableObjectsOnSpawn = false;

        /// <summary>
        /// Enables detailed logging
        /// </summary>
        public bool VerboseLogging = false;
    }
}
