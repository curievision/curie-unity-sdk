using Newtonsoft.Json;
using Siccity.GLTFUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace CurieSDK
{
    public class Curie
    {

        #region Public Fields / Properties

        /// <summary>
        /// Singleton instance of Curie Api
        /// </summary>
        public static Curie Instance = new Curie();

        /// <summary>
        /// Runtime modifiable settings for Curie SDK
        /// </summary>
        public static CurieSettings Settings = new CurieSettings();

        /// <summary>
        /// Called when Curie API has initialised
        /// </summary>
        public static event EventHandler OnInitialized
        {
            add => Instance._onInitialized += value;
            remove => Instance._onInitialized -= value;
        }

        /// <summary>
        /// Returns true if the API is ready to be used
        /// </summary>
        public bool Initialised { get; private set; }

        /// <summary>
        /// Last endpoint called for development purposes
        /// </summary>
        public string LastDevCall
        {
            get { return _lastDevCall; }
        }

        /// <summary>
        /// Last JSON response output for development purposes
        /// </summary>
        public string LastDevOutput
        {
            get { return _lastDevOutput; }
        }
        #endregion

        #region Private Fields / Properties

        private string _pubKey;
        private event EventHandler _onInitialized;

        private CurieMono _handler;

        private string _lastDevCall;
        private string _lastDevOutput;

        private Dictionary<string, Product> _cachedProductData = new Dictionary<string, Product>();
        private Dictionary<string, ModelResultHolder> _cachedProductFileData = new Dictionary<string, ModelResultHolder>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialises the Curie SDK
        /// </summary>
        /// <param name="publicKey">Public key found in Curie Studio</param>
        /// <returns></returns>
        public static Curie Init(string publicKey)
        {
            return Init(publicKey, null);   
        }

        /// <summary>
        /// Initialises the Curie SDK with the specified settings
        /// </summary>
        /// <param name="publicKey">Public key found in Curie Studio</param>
        /// <returns></returns>
        public static Curie Init(string publicKey, CurieSettings settings)
        {
            if(settings == null) settings = new CurieSettings();

            Instance._pubKey = publicKey;
            Settings = settings;

#if UNITY_WEBGL
            Settings.CacheModels = false;
#endif

            var handler = Instance.GetRoutineHandler();
            handler.StartCoroutine(Instance.API_Initialise());
            return Instance;
        }

        /// <summary>
        /// Search for products in your organization with optional query filter
        /// </summary>
        /// <param name="searchQuery">Optional search string to filter items by</param>
        /// <param name="OnComplete">Callback method when the api call completes</param>
        /// <returns></returns>
        public CurieAPICall<List<Product>> SearchProducts(string searchQuery = "", Action<List<Product>> OnComplete = null)
        {
            var apiCall = new CurieAPICall<List<Product>>(_handler, API_SearchProducts(searchQuery, -1, -1), OnComplete);
            return apiCall;
        }

        /// <summary>
        /// Search for products in your organization with query filter, skip and limit
        /// </summary>
        /// <param name="searchQuery">Search string to filter items by</param>
        /// <param name="skip">Number of results to skip</param>
        /// <param name="limit">Max number of products returned</param>
        /// <param name="OnComplete">Callback method when the api call completes</param>
        /// <returns></returns>
        public CurieAPICall<List<Product>> SearchProducts(string searchQuery, int skip, int limit, Action<List<Product>> OnComplete = null)
        {
            var apiCall = new CurieAPICall<List<Product>>(_handler, API_SearchProducts(searchQuery, skip, limit), OnComplete);
            return apiCall;
        }

        /// <summary>
        /// Get product metadata (name, desc, brand, thumbnail url). Will used cached metadata if available
        /// </summary>
        /// <param name="modelId">Model ID</param>
        /// <param name="OnComplete">Callback method when the api call completes</param>
        /// <returns></returns>
        public CurieAPICall<Product> GetProductData(string modelId, Action<Product> OnComplete = null)
        {
            var apiCall = new CurieAPICall<Product>(_handler, API_GetProductData(modelId), OnComplete);
            return apiCall;
        }

        /// <summary>
        /// Instantiates a Curie model by ID
        /// </summary>
        /// <param name="modelId">Model ID</param>
        /// <param name="OnComplete">Callback method when the api call completes</param>
        /// <returns></returns>
        public CurieAPICall<GameObject> InstantiateModel(string modelId, Action<GameObject> OnComplete = null)
        {
            var apiCall = new CurieAPICall<GameObject>(_handler, API_InstantiateModel(modelId), OnComplete);
            return apiCall;
        }

        #endregion

        #region Private Methods
        /// <summary>
        /// Get product file data
        /// </summary>
        /// <param name="modelId">Model ID</param>
        /// <param name="OnComplete">Callback method when the api call completes</param>
        /// <returns></returns>
        private CurieAPICall<ModelResultHolder> GetProductFiles(string modelId, Action<ModelResultHolder> OnComplete = null)
        {
            var apiCall = new CurieAPICall<ModelResultHolder>(_handler, API_GetProductFiles(modelId), OnComplete);
            return apiCall;
        }

        private Product GetCachedProductData(string modelId)
        {
            var productData = _cachedProductData.ContainsKey(modelId) ? _cachedProductData[modelId] : null;
            return productData;
        }

        private ModelResultHolder GetCachedProductFileData(string modelId)
        {
            var productData = _cachedProductFileData.ContainsKey(modelId) ? _cachedProductFileData[modelId] : null;
            return productData;
        }

        /// </summary>
        /// <returns></returns>
        private CurieMono GetRoutineHandler()
        {
            if (_handler == null)
            {
                var handlerObj = new GameObject("[Curie]");
                _handler = handlerObj.AddComponent<CurieMono>();
            }

            return _handler;
        }

        private void SetLastDevCall(string uri, string type = "GET")
        {
            _lastDevCall = string.Format("{0}: {1}", type, uri);
        }

        private void SetLastDevOutput(string resultJson)
        {
            var parsedJson = JsonConvert.DeserializeObject(resultJson);
            var prettified = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            _lastDevOutput = prettified;
        }


        private string GetCachedModelFile(string modelId)
        {
            var basePath = Path.Combine(Application.persistentDataPath, "CurieModels/");
            Directory.CreateDirectory(basePath);

            var modelPath = "";
            string[] files = Directory.GetFiles(basePath);
            foreach (string file in files)
            {
                var fileName = Path.GetFileName(file);
                if (fileName.StartsWith(modelId))
                {
                    modelPath = file;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(modelPath))
            {
                Console.WriteLine(Path.GetFileName(modelPath));
                return modelPath;
            }

            return null;
        }

        private string GetSavePath(string modelId)
        {
            var basePath = Path.Combine(Application.persistentDataPath, "CurieModels/");
            Directory.CreateDirectory(basePath);

            var savePath = Path.Combine(basePath, modelId + ".glb");
            return savePath;
        }
        #endregion

        #region Endpoints

        /// <summary>
        /// Initialise Curie API
        /// </summary>
        private IEnumerator API_Initialise()
        {
            Debug.Log("[Curie] Successfully authenticated with Curie API");
            Initialised = true;
            _onInitialized?.Invoke(this, null);
            yield return true;
        }

        /// <summary>
        /// Use the API to load a model's metadata including thumbnail (if it exists) and cache it by refId
        /// </summary>
        private IEnumerator API_SearchProducts(string productName, int skip, int limit)
        {
            if (!Initialised)
            {
                if(Settings.VerboseLogging) Debug.Log("[Curie] Initialise Curie SDK first with Setup.Init before making API calls");
                yield break;
            }

            if (skip == -1)
                skip = 0;
            if (limit == -1)
                limit = 10;

            var api_url = string.Format("https://api.curie.io/public/products?skip={0}&limit={1}&search={2}&sort=-created_on", skip, limit, productName);
            SetLastDevCall(api_url);

            using (UnityWebRequest webRequest = UnityWebRequest.Get(api_url))
            {
var curieApiKey = _pubKey;
                webRequest.SetRequestHeader("x-curie-api-key", curieApiKey);
                webRequest.SetRequestHeader("accept", "application/json");

                Debug.Log("api key: " + curieApiKey);

                yield return webRequest.SendWebRequest();

#pragma warning disable CS0618 // Type or member is obsolete
                if (!webRequest.isHttpError)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    Debug.Log("good result!");
                    var resultJson = webRequest.downloadHandler.text;

                    Debug.Log(resultJson);
                    SetLastDevOutput(resultJson);
                    var resultObj = JsonUtility.FromJson<ProductList>(resultJson);
                    Debug.Log("products::" + resultObj.products.Count);
                    var searchResults = resultObj.products;

                    // Cache product data
                    foreach(var product in searchResults)
                    {
                        if (!_cachedProductData.ContainsKey(product.id))
                            _cachedProductData.Add(product.id, product);
                    }
                    

                    // Return the result
                    yield return searchResults;
                }
                else
                {
                    Debug.Log(webRequest.error);
                }
            }
        }

        /// Get product by ID
        /// <summary>
        /// Use the API to load a model's metadata including thumbnail (if it exists) and cache it by refId
        /// </summary>
        private IEnumerator API_GetProductData(string model)
        {
            yield return null;
            if (!Initialised)
            {
                if(Settings.VerboseLogging) Debug.Log("[Curie] Initialise Curie SDK first with Setup.Init before making API calls");
                yield break;
            }

            if(_cachedProductData.ContainsKey(model))
            {
                Debug.Log("caaaccched");
                yield return _cachedProductData[model];
                yield break;
            }

            var api_url = string.Format("https://api.curie.io/public/products/{0}", model);
            SetLastDevCall(api_url);
            using (UnityWebRequest webRequest = UnityWebRequest.Get(api_url))
            {
var curieApiKey = _pubKey;
                webRequest.SetRequestHeader("x-curie-api-key", curieApiKey);
                webRequest.SetRequestHeader("accept", "application/json");
                yield return webRequest.SendWebRequest();

#pragma warning disable CS0618 // Type or member is obsolete
                if (!webRequest.isHttpError)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    var resultJson = webRequest.downloadHandler.text;
                    SetLastDevOutput(resultJson);
                    var productData = JsonUtility.FromJson<Product>(resultJson);

                    if (!_cachedProductData.ContainsKey(model))
                        _cachedProductData.Add(model, productData);

                    yield return productData;
                }
            }
        }


        /// <summary>
        /// Use the API to load a model's metadata including thumbnail (if it exists) and cache it by refId
        /// </summary>
        private IEnumerator API_GetProductFiles(string model)
        {
            yield return null;
            if (!Initialised)
            {
                if(Settings.VerboseLogging) Debug.Log("[Curie] Initialise Curie SDK first with Setup.Init before making API calls");
                yield break;
            }

            var api_url = string.Format("https://api.curie.io/public/products/{0}/media", model);
            SetLastDevCall(api_url);
            using (UnityWebRequest webRequest = UnityWebRequest.Get(api_url))
            {
var curieApiKey = _pubKey;
                webRequest.SetRequestHeader("x-curie-api-key", curieApiKey);
                webRequest.SetRequestHeader("accept", "application/json");
                yield return webRequest.SendWebRequest();

#pragma warning disable CS0618 // Type or member is obsolete
                if (!webRequest.isHttpError)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    var resultJson = webRequest.downloadHandler.text;
                    var resultObj = JsonUtility.FromJson<ModelResultHolder>("{\"media\":" + resultJson + "}");
                    SetLastDevOutput(resultJson);
                    if (!_cachedProductFileData.ContainsKey(model))
                        _cachedProductFileData.Add(model, resultObj);
                    else
                        _cachedProductFileData[model] = resultObj;

                    yield return resultObj;
                }
            }
        }

        /// <summary>
        /// Use the API to load a model's metadata including thumbnail (if it exists) and cache it by refId
        /// </summary>
        private IEnumerator API_AddView(string model, string mediaId)
        {
            yield return null;
            if (!Initialised)
            {
                if(Settings.VerboseLogging) Debug.Log("[Curie] Initialise Curie SDK first with Setup.Init before making API calls");
                yield break;
            }

            var api_url = string.Format("https://api.curie.io/public/products/{0}/media/{1}", model, mediaId);
            SetLastDevCall(api_url);
            using (UnityWebRequest webRequest = UnityWebRequest.Head(api_url))
            {
var curieApiKey = _pubKey;
                webRequest.SetRequestHeader("x-curie-api-key", curieApiKey);
                webRequest.SetRequestHeader("accept", "application/json");

                //-H 'x-curie-api-key: amGTFYSraYIABwBNQDeT_uwSErVwHPDi'
                yield return webRequest.SendWebRequest();

#pragma warning disable CS0618 // Type or member is obsolete
                if (webRequest.isHttpError)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    if(Settings.VerboseLogging) Debug.LogError("[Curie] Error logging view to Curie API");
                }
                else
                {
                    if(Settings.VerboseLogging) Debug.Log("[Curie] View Logged - Product ID: " + model + " | Media ID: " + mediaId);
                }
            }
        }

        /// <summary>
        /// Use the API to load a model and instantiate it into the scene by model id
        /// </summary>
        private IEnumerator API_InstantiateModel(string modelId)
        {
            GameObject modelFromApi = null;

            var cachedVersionPath = Settings.CacheModels ? GetCachedModelFile(modelId) : "";
            var exists = !string.IsNullOrEmpty(cachedVersionPath);
            byte[] resultBytes = new byte[0];
            var fileSize = 0;

            if (!exists)
            {
                var productFileData = GetCachedProductFileData(modelId);
                if (productFileData == null)
                    yield return _handler.StartCoroutine(API_GetProductFiles(modelId));

                productFileData = GetCachedProductFileData(modelId);
                if (productFileData == null)
                {
                    if(Settings.VerboseLogging) Debug.LogError("[Curie] Product data does not exist or was not found. ModelId:" + modelId);
                    yield break;
                }

                var resultModel = productFileData.media.FirstOrDefault(m => m.format == "glb");
                if (resultModel == null)
                {
                    if(Settings.VerboseLogging) Debug.LogError("[Curie] No valid GLB model type found for model with ID: " + modelId);

                    yield return null;
                    yield break;
                }
                var resultUrl = resultModel.url;

                if (string.IsNullOrEmpty(resultUrl))
                {
                    if(Settings.VerboseLogging) Debug.LogError("[Curie] Error getting url for model with ID: " + modelId);
                    yield return null;
                    yield break;
                }

                using (UnityWebRequest webRequest = UnityWebRequest.Get(resultUrl))
                {
                    yield return webRequest.SendWebRequest();
                    resultBytes = webRequest.downloadHandler.data;

                    if (Settings.CacheModels)
                    {
                        var savePath = GetSavePath(modelId + "-" + resultModel.media_id);
                        File.WriteAllBytes(savePath, resultBytes);
                    }
                }
            }

            var waiting = true;
            GameObject gameObj = null;
            if (!exists)
            {
                fileSize = resultBytes.Length;


#if UNITY_WEBGL
                waiting = false;
                gameObj = Importer.LoadFromBytes(resultBytes, new ImportSettings());

#else

                Importer.ImportGLBAsync(resultBytes, new ImportSettings(),
                    (a, anims) =>
                    {
                        waiting = false;
                        gameObj = a;
                    },
                (a) => {

                });
#endif
            }
            else
            {
                if (Settings.VerboseLogging) Debug.Log("Loaded cached version.");
                Importer.LoadFromFileAsync(cachedVersionPath, new ImportSettings(),
                    (a, anims) =>
                    {
                        waiting = false;
                        gameObj = a;    
                    },
                (a) => {

                });

                fileSize = (int)new System.IO.FileInfo(cachedVersionPath).Length;
                var props = Path.GetFileNameWithoutExtension(cachedVersionPath).Split('-');
                var productId = props[0];
                var productMediaId = props[1];

                //Handle view increment in background
                _handler.StartCoroutine(API_AddView(productId, productMediaId));
            }

            while (waiting)
            {
                yield return null;
            }


            if (gameObj == null)
            {
                if (Settings.VerboseLogging) Debug.LogError("[Curie] Error importing the object into the scene.");
                yield return null;
                yield break;
            }

            if (Settings.DisableObjectsOnSpawn)
                gameObj.SetActive(false);

            modelFromApi = gameObj;
            if (Settings.ResetModelsToCenter)
            {
                gameObj.SetActive(true);
                var modelObj = CurieModelTools.RepositionModel(gameObj);

                if (Settings.DisableObjectsOnSpawn)
                    modelObj.SetActive(false);

                modelObj.name = "[CurieModel] " + modelId;
                if(Settings.VerboseLogging) Debug.Log("[Curie] Model (" + modelId + ") loaded from API");
                modelFromApi = modelObj;
            }

            var tracker = modelFromApi.AddComponent<CurieModelTracker>();
            tracker.FileSize = fileSize;
            tracker.ModelId = modelId;


            yield return modelFromApi;
        }

        #endregion
    }
}
