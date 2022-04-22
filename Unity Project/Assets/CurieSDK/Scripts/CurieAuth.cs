using UnityEngine;

namespace CurieSDK.Examples
{
    public class CurieAuth : MonoBehaviour
    {
        [SerializeField] private string _publicKey;
        [SerializeField] private string _apiKey;
        [SerializeField] private CurieSettings _settings;

        void Start()
        {
            Curie.Init(_publicKey, _apiKey, _settings);
        }
    }
}
