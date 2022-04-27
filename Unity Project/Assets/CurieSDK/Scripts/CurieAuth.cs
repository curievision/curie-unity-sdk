using UnityEngine;

namespace CurieSDK.Examples
{
    /// <summary>
    /// Simple Curie SDK Auth class. Initialises Curie SDK with public key and specified settings.
    /// </summary>
    public class CurieAuth : MonoBehaviour
    {
        [SerializeField] private string _publicKey;
        [SerializeField] private CurieSettings _settings;

        void Start()
        {
            Curie.Init(_publicKey, _settings);
        }
    }
}
