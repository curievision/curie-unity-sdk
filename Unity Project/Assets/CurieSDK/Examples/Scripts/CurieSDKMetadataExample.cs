using CurieSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CurieSDK.Examples
{
    public class CurieSDKMetadataExample : MonoBehaviour
    {
        private Curie curie;

        public Image ProductImage;
        public TextMeshProUGUI ProductBrand;
        public TextMeshProUGUI ProductName;
        public TextMeshProUGUI ProductDesc;

        void Awake()
        {
            // Add event handler for OnInitialize (must be in awake)
            Curie.OnInitialized += API_OnInitialised;
        }

        private void API_OnInitialised(object sender, EventArgs e)
        {
            // Store a reference to the Curie API Singleton instance
            curie = Curie.Instance;

            // Search for all products
            curie.SearchProducts("", OnSearchedProducts);
        }

        private void OnSearchedProducts(List<Product> obj)
        {
            Debug.Log("Products Found:" + obj.Count);

            // Load Product Metadata for the first model found
            StartCoroutine(LoadProductMetaData(obj[0].id));
        }

        private IEnumerator LoadProductMetaData(string productId)
        {
            // First instantiate the model
            var modelApiCall = curie.InstantiateModel(productId);
            yield return modelApiCall.Coroutine;
            var modelGameObj = modelApiCall.GetResult();

            // Load the product metadata ( this will already be cached)
            var modelMetaDataCall = curie.GetProductData(productId);
            yield return modelMetaDataCall.Coroutine;
            var modelMetaData = modelMetaDataCall.GetResult();

            // Load the image and set the text values
            yield return StartCoroutine(SetImage(ProductImage, modelMetaData.thumbnail_url));   
            ProductBrand.text = "Brand: " + modelMetaData.brand;
            ProductName.text = modelMetaData.name;
            ProductDesc.text = modelMetaData.description;
        }

        /// <summary>
        /// Downloads the image by URL and applies it to a UnityUI.Image object
        /// </summary>
        private IEnumerator SetImage(Image image, string url)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();
                var resultBytes = webRequest.downloadHandler.data;
                var t = new Texture2D(0, 0);
                t.LoadImage(resultBytes);
                Sprite sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f));
                image.sprite = sprite;
            }
        }

    }
}
