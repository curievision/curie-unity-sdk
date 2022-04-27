using CurieSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CurieSDK.Examples
{
    public class CurieSDKExample : MonoBehaviour
    {
        private Curie curie;

        void Awake()
        {        
            // Add event handler for OnInitialize (must be in awake)
            Curie.OnInitialized += API_OnInitialised;
        }

        private void API_OnInitialised(object sender, EventArgs e)
        {
            curie = Curie.Instance;
            curie.SearchProducts("", OnSearchedProducts);
        }

        private void OnSearchedProducts(List<Product> obj)
        {
            Debug.Log("Products Found:" + obj.Count);
            curie.InstantiateModel(obj[0].id);
        }
    }
}
