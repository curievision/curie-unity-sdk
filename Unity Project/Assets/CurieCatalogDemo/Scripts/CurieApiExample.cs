using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Siccity.GLTFUtility;
using UnityEngine.Networking;
using CurieSDK;
using System;
using System.Linq;
using Newtonsoft.Json;
using CurieSDK;

public class CurieApiExample : MonoBehaviour
{
    [SerializeField] private string _publicKey;
    [SerializeField] private string _secretKey;

    [SerializeField] private bool _bearerCached;
    [SerializeField] private string _bearerToken;

    [SerializeField] private bool _resetModelsToCenter = true;
    [SerializeField] private bool _disableObjectsOnSpawn = true;

    [SerializeField] private Dictionary<string, ModelResultHolder> _productData;
    [SerializeField] private Dictionary<string, Product> _productDataNew;
    [SerializeField] private Dictionary<string, GameObject> _spawnedModels;
    public List<string> SearchResults;


    public string LastDevCall = "https://test";
    public string LastDevOutput = "{Test: 'test'}";

    private void Start()
    {
        _spawnedModels = new Dictionary<string,GameObject>();
        _productData = new Dictionary<string,ModelResultHolder>();
        _productDataNew = new Dictionary<string, Product>();
        StartCoroutine(GetAndCacheBearerToken());
    }

    /// <summary>
    /// Get a previously spawned model by refId assigned when calling InstantiateModel
    /// </summary>
    /// <param name="refId"></param>
    /// <returns>The model by Ref ID or null</returns>
    public GameObject GetSpawnedModel(string refId)
    {
        if (_spawnedModels.ContainsKey(refId))
        {
            var model = _spawnedModels[refId];
            model.SetActive(true);
            return model;
        }

        return null;
    }

    /// <summary>
    /// Get product data if it has been loaded
    /// </summary>
    /// <param name="refId">The ref id used when loading the product from the api</param>
    /// <returns>The product data or null</returns>
    public ModelResultHolder GetCachedProductData(string refId)
    {
        if (_productData.ContainsKey(refId))
        {
            return _productData[refId];
        }

        return null;
    }

    /// <summary>
    /// Get product data if it has been loaded
    /// </summary>
    /// <param name="refId">The ref id used when loading the product from the api</param>
    /// <returns>The product data or null</returns>
    public Product GetCachedProductDataNew(string refId)
    {
        if (_productDataNew.ContainsKey(refId))
        {
            return _productDataNew[refId];
        }

        return null;
    }


    /// moved
    /// <summary>
    /// Authenticate using public and secret key which returns a bearer key to be used in API requests
    /// </summary>
    private IEnumerator GetAndCacheBearerToken()
    {
        _bearerToken = "";
        var api_auth_url = "https://api.curie.io/auth";

        var data = new Dictionary<string, string>()
        {
            {"grant_type"   , "client_credentials"},
            {"client_id"    , _publicKey },
            {"client_secret", _secretKey},
        };

        using (UnityWebRequest webRequest = UnityWebRequest.Post(api_auth_url, data))
        {
            yield return webRequest.SendWebRequest();

#pragma warning disable CS0618 // Type or member is obsolete
            if (!webRequest.isHttpError)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                var resultJson = webRequest.downloadHandler.text;
                var resultObj = JsonUtility.FromJson<CurieBearerToken>(resultJson);
                var bearer = resultObj.access_token;
                _bearerToken = bearer;
                _bearerCached = true;
            }
        }
    }

    /// <summary>
    /// Use the API to load a model and instantiate it into the scene by model id
    /// </summary>
    public IEnumerator InstantiateModel(string modelId, string refId)
    {
        GameObject modelFromApi = null;

        yield return StartCoroutine(GetProduct(modelId, refId));
        var resultObj = GetCachedProductData(refId);
        if(resultObj == null)
        {
            Debug.LogError("[Curie] Product data does not exist or was not loaded. RefId:" + refId);
            yield break;
        }

        var resultModel = resultObj.media.FirstOrDefault(m => m.format == "glb");
        if (resultModel == null)
        {
            Debug.LogError("[Curie] No valid GLB model type found for model with ID: " + modelId);

            yield return null;
            yield break;
        }
        var resultUrl = resultModel.url;

        if (string.IsNullOrEmpty(resultUrl))
        {
            Debug.LogError("[Curie] Error getting url for model with ID: " + modelId);
            yield return null;
            yield break;
        }

        using (UnityWebRequest webRequest = UnityWebRequest.Get(resultUrl))
        {
            yield return webRequest.SendWebRequest();
            var resultBytes = webRequest.downloadHandler.data;
            //try
            //{
                var waiting = true;
                GameObject gameObj = null;
                Importer.ImportGLBAsync(resultBytes, new ImportSettings(),
                    (a, anims) =>
                    {
                        waiting = false;
                        gameObj = a;
                    },
                (a) => {

                });

                while (waiting)
                {
                    yield return null;
                }


                if (gameObj == null)
                {
                    Debug.LogError("Null gameobject....");
                    yield return null;
                    yield break;
                }


                if(_disableObjectsOnSpawn)
                    gameObj.SetActive(false);

                modelFromApi = gameObj;
                Debug.Log("Object created");
                if (_resetModelsToCenter)
                {
                    gameObj.SetActive(true);
                    var modelObj = CurieModelFix.RepositionModel(gameObj);
                    modelObj.SetActive(false);
                    modelObj.name = "CurieModel_" + modelId;
                    modelFromApi = modelObj;
                }

                var tracker = modelFromApi.AddComponent<CurieModelTracker>();
                tracker.FileSize = resultBytes.Length;
                tracker.ModelId = modelId;
            //}
            //catch (Exception e)
            //{
            //    Debug.LogException(e);
            //    Debug.Log("[Curie] Error instantiating model with ID: " + modelId);
            //}
        }

        if (!_spawnedModels.ContainsKey(refId))
            _spawnedModels.Add(refId, modelFromApi);
        else
            _spawnedModels[refId] = modelFromApi;

        yield return modelFromApi;
    }


    /// <summary>
    /// Use the API to load a model and instantiate it using pre-cached model data and then cache the 
    /// spawned gameobject by refId
    /// </summary>
    public IEnumerator InstantiateModelFromProductData(ModelResultHolder modelData, string refId)
    {
        GameObject modelFromApi = null;
        var productDataExists = _productData.ContainsValue(modelData);
        var modelId = productDataExists ? _productData.First(p => p.Value == modelData).Key : "RefId_" + refId;

        var productData = modelData;
        if (productData == null)
        {
            Debug.LogError("[Curie] Product data does not exist or was not loaded. RefId:" + refId);
            yield break;
        }

        var resultModel = productData.media.FirstOrDefault(m => m.format == "glb");
        if (resultModel == null)
        {
            Debug.LogError("[Curie] No valid GLB model type found for model " + modelId + " with RefID: " + refId);

            yield return null;
            yield break;
        }
        var resultUrl = resultModel.url;

        if (string.IsNullOrEmpty(resultUrl))
        {
            Debug.LogError("[Curie] Error getting url for model " + modelId + " with RefID: " + refId);
            yield return null;
            yield break;
        }

        using (UnityWebRequest webRequest = UnityWebRequest.Get(resultUrl))
        {
            yield return webRequest.SendWebRequest();
            var resultBytes = webRequest.downloadHandler.data;

                var waiting = true;
                GameObject gameObj = null;
                Importer.ImportGLBAsync(resultBytes, new ImportSettings(),
                    (a, anims) =>
                    {
                        waiting = false;
                        gameObj = a;
                    },
                (a) => {

                });

                while (waiting)
                {
                    yield return null;
                }


                if (gameObj == null)
                {
                    Debug.LogError("Null gameobject....");
                    yield return null;
                    yield break;
                }

            try
            {
                if (_disableObjectsOnSpawn)
                    gameObj.SetActive(false);

                modelFromApi = gameObj;

                if (_resetModelsToCenter)
                {
                    gameObj.SetActive(true);
                    var modelObj = CurieModelFix.RepositionModel(gameObj);
                    modelObj.SetActive(false);
                    modelObj.name = "CurieModel_" + modelId;
                    modelFromApi = modelObj;
                }


                var tracker = modelFromApi.AddComponent<CurieModelTracker>();
                tracker.FileSize = resultBytes.Length;
                tracker.ModelId = modelId;
            }
            catch (Exception)
            {
                Debug.Log("[Curie] Error instantiating model " + modelId + " with Ref ID: " + modelId);
            }
        }

        if (!_spawnedModels.ContainsKey(refId))
            _spawnedModels.Add(refId, modelFromApi);
        else
            _spawnedModels[refId] = modelFromApi;

        yield return modelFromApi;
    }


    /// <summary>
    /// Use the API to load a model's metadata including thumbnail (if it exists) and cache it by refId
    /// </summary>
    public IEnumerator GetProduct(string model, string refId)
    {
        if (!_bearerCached)
        {
            Debug.Log("[Curie] Authenticating with Curie API and Getting bearer token");
            yield return StartCoroutine(GetAndCacheBearerToken());
        }

        var api_url = string.Format("https://api.curie.io/admin/v1/products/{0}/media", model);
        SetLastDevCall(api_url);
        using (UnityWebRequest webRequest = UnityWebRequest.Get(api_url))
        {
            var token = "Bearer " + _bearerToken;
            webRequest.SetRequestHeader("Authorization", token);
            webRequest.SetRequestHeader("accept", "application/json");
            yield return webRequest.SendWebRequest();

#pragma warning disable CS0618 // Type or member is obsolete
            if (!webRequest.isHttpError)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                var resultJson = webRequest.downloadHandler.text;
                var resultObj = JsonUtility.FromJson<ModelResultHolder>("{\"media\":" + resultJson + "}");
                SetLastDevOutput(resultJson);
                if (!_productData.ContainsKey(refId))
                    _productData.Add(refId, resultObj);
                else
                    _productData[refId] = resultObj;
                
            }
        }
    }

    private void SetLastDevCall(string uri, string type = "GET")
    {
        LastDevCall = string.Format("{0}: {1}", type, uri);
    }
    private void SetLastDevOutput(string resultJson)
    {
        var parsedJson = JsonConvert.DeserializeObject(resultJson);
        var prettified = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        LastDevOutput = prettified;
    }

    /// <summary>
    /// Use the API to load a model's metadata including thumbnail (if it exists) and cache it by refId
    /// </summary>
    public IEnumerator SearchProduct(string productName)
    {
        if (!_bearerCached)
        {
            Debug.Log("[Curie] Authenticating with Curie API and Getting bearer token");
            yield return StartCoroutine(GetAndCacheBearerToken());
        }

        var api_url = string.Format("https://api.curie.io/admin/v1/products?skip=0&limit=15&search={0}&sort=-created_on", productName);
        SetLastDevCall(api_url);

        using (UnityWebRequest webRequest = UnityWebRequest.Get(api_url))
        {
            var token = "Bearer " + _bearerToken;
            webRequest.SetRequestHeader("Authorization", token);
            webRequest.SetRequestHeader("accept", "application/json");
            yield return webRequest.SendWebRequest();

#pragma warning disable CS0618 // Type or member is obsolete
            if (!webRequest.isHttpError)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                var resultJson = webRequest.downloadHandler.text;
                SetLastDevOutput(resultJson);

                Debug.Log("SEARCH OUTPUT:::\n\n\n" + resultJson);
                var resultObj = JsonUtility.FromJson<ProductList>(resultJson);
                var searchResults = resultObj.products.Select(p => p.id).ToList();
                SearchResults = searchResults;
            }
        }
    }

    //  '' \


    /// <summary>
    /// Use the API to load a model's metadata including thumbnail (if it exists) and cache it by refId
    /// </summary>
    public IEnumerator GetProductNew(string model, string refId)
    {
        if (!_bearerCached)
        {
            Debug.Log("[Curie] Authenticating with Curie API and Getting bearer token");
            yield return StartCoroutine(GetAndCacheBearerToken());
        }

        var api_url = string.Format("https://api.curie.io/admin/v1/products/{0}", model);
        SetLastDevCall(api_url);
        using (UnityWebRequest webRequest = UnityWebRequest.Get(api_url))
        {
            var token = "Bearer " + _bearerToken;
            webRequest.SetRequestHeader("Authorization", token);
            webRequest.SetRequestHeader("accept", "application/json");
            yield return webRequest.SendWebRequest();

#pragma warning disable CS0618 // Type or member is obsolete
            if (!webRequest.isHttpError)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                var resultJson = webRequest.downloadHandler.text;
                SetLastDevOutput(resultJson);
                var resultObj = JsonUtility.FromJson<Product>(resultJson);

                if (!_productDataNew.ContainsKey(refId))
                    _productDataNew.Add(refId, resultObj);
                else
                    _productDataNew[refId] = resultObj;

            }
        }
    }

    IEnumerator ImportObjectFromURL(string url, string sname = "test.gltf")
    {
        WWW www = new WWW(url);
        yield return www;
        string writePath = Application.dataPath + "/gltf_files/" + sname;
        System.IO.File.WriteAllBytes(writePath, www.bytes);
        yield return new WaitForSeconds(1);
        ImportGLTFAsync(writePath);
    }
    void ImportGLTFAsync(string filepath)
    {
        Importer.ImportGLTFAsync(filepath, new ImportSettings(), OnFinishAsync);
    }

    private void OnFinishAsync(GameObject arg1, AnimationClip[] arg2)
    {
        Debug.Log("Finished importing " + arg1.name);
    }

}
