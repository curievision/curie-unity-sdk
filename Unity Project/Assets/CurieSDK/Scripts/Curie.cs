using CurieSDK;
using Newtonsoft.Json;
using Siccity.GLTFUtility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace CurieSDK
{
    public class Curie
    {

        ////////////////////////////////////////////// PUBLIC FIELDS/PROPERTIES

        public static Curie Instance = new Curie();
        public static CurieSettings Settings = new CurieSettings();

        public static event EventHandler OnInitialized
        {
            add => Instance._onInitialized += value;
            remove => Instance._onInitialized -= value;
        }

        public bool Initialised { get; private set; }



        public string LastDevCall
        {
            get { return _lastDevCall; }
        }
        public string LastDevOutput
        {
            get { return _lastDevOutput; }
        }

        ////////////////////////////////////////////// PRIVATE FIELDS/PROPERTIES

        private string _pubKey;
        private string _bearerToken;
        private event EventHandler _onInitialized;

        private CurieMono _handler;

        private string _lastDevCall;
        private string _lastDevOutput;

        private Dictionary<string, Product> _cachedProductData = new Dictionary<string, Product>();
        private Dictionary<string, ModelResultHolder> _cachedProductFileData = new Dictionary<string, ModelResultHolder>();

        ////////////////////////////////////////////// PUBLIC METHODS
        public static Curie Init(string publicKey, string apiKey)
        {
            return Init(publicKey, apiKey, null);   
        }
        public static Curie Init(string publicKey, string apiKey, CurieSettings settings)
        {
            if(settings == null) settings = new CurieSettings();

            Instance._pubKey = publicKey;
            Settings = settings;


#if UNITY_WEBGL
            Settings.CacheModels = false;
#endif


            var handler = Instance.GetRoutineHandler();
            handler.StartCoroutine(Instance.API_GetAndCacheBearerToken(publicKey, apiKey));
            return Instance;
        }

        public CurieAPICall<List<Product>> SearchProducts(string searchQuery, Action<List<Product>> OnComplete = null)
        {
            var apiCall = new CurieAPICall<List<Product>>(_handler, API_SearchProducts(searchQuery), OnComplete);
            return apiCall;
        }

        public CurieAPICall<Product> GetProductData(string modelId, Action<Product> OnComplete = null)
        {
            var apiCall = new CurieAPICall<Product>(_handler, API_GetProductData(modelId), OnComplete);
            return apiCall;
        }

        public CurieAPICall<ModelResultHolder> GetProductFiles(string modelId, Action<ModelResultHolder> OnComplete = null)
        {
            var apiCall = new CurieAPICall<ModelResultHolder>(_handler, API_GetProductFiles(modelId), OnComplete);
            return apiCall;
        }

        public CurieAPICall<GameObject> InstantiateModel(string modelId, Action<GameObject> OnComplete = null)
        {
            var apiCall = new CurieAPICall<GameObject>(_handler, API_InstantiateModel(modelId), OnComplete);
            return apiCall;
        }


        public Product GetCachedProductData(string modelId)
        {
            var productData = _cachedProductData.ContainsKey(modelId) ? _cachedProductData[modelId] : null;
            return productData;
        }

        public ModelResultHolder GetCachedProductFileData(string modelId)
        {
            var productData = _cachedProductFileData.ContainsKey(modelId) ? _cachedProductFileData[modelId] : null;
            return productData;
        }


        ///////////////////////////////////////////// PRIVATE METHODS
        private CurieMono GetRoutineHandler()
        {
            if (_handler == null)
            {
                var handlerObj = new GameObject("[Curie]");
                _handler = handlerObj.AddComponent<CurieMono>();
            }

            return _handler;
        }

        ///////////////////////// ENDPOINTS


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


        /// <summary>
        /// Authenticate using public and secret key which returns a bearer key to be used in API requests
        /// </summary>
        private IEnumerator API_GetAndCacheBearerToken(string _publicKey, string _secretKey)
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
                    Initialised = true;

                    Debug.Log("[Curie] Successfully authenticated with Curie API");
                    _onInitialized?.Invoke(this, null);
                }
            }
        }

        /// <summary>
        /// Use the API to load a model's metadata including thumbnail (if it exists) and cache it by refId
        /// </summary>
        public IEnumerator API_SearchProducts(string productName)
        {
            if (!Initialised)
            {
                if(Settings.VerboseLogging) Debug.Log("[Curie] Initialise Curie SDK first with Setup.Init before making API calls");
                yield break;
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
                    var resultObj = JsonUtility.FromJson<ProductList>(resultJson);
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
            }
        }

        /// Get product by ID
        /// <summary>
        /// Use the API to load a model's metadata including thumbnail (if it exists) and cache it by refId
        /// </summary>
        public IEnumerator API_GetProductData(string model)
        {
            yield return null;
            if (!Initialised)
            {
                if(Settings.VerboseLogging) Debug.Log("[Curie] Initialise Curie SDK first with Setup.Init before making API calls");
                yield break;
            }

            if(_cachedProductData.ContainsKey(model))
            {
                yield return _cachedProductData[model];
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
        public IEnumerator API_GetProductFiles(string model)
        {
            yield return null;
            if (!Initialised)
            {
                if(Settings.VerboseLogging) Debug.Log("[Curie] Initialise Curie SDK first with Setup.Init before making API calls");
                yield break;
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
        public IEnumerator API_AddView(string model, string mediaId)
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
                webRequest.SetRequestHeader("accept", "application/json");
                webRequest.SetRequestHeader("x-curie-api-key", _pubKey);

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
        public IEnumerator API_InstantiateModel(string modelId)
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

                Importer.ImportGLBAsync(resultBytes, new ImportSettings(),
                    (a, anims) =>
                    {
                        waiting = false;
                        gameObj = a;
                    },
                (a) => {

                });
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
                var modelObj = CurieModelFix.RepositionModel(gameObj);

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

        private string GetCachedModelFile(string modelId)
        {
            var basePath = Path.Combine(Application.persistentDataPath, "CurieModels/");
            Directory.CreateDirectory(basePath);

            var modelPath = "";
            string[] files = Directory.GetFiles(basePath);
            foreach (string file in files)
            {
                var fileName = Path.GetFileName(file);  
                if(fileName.StartsWith(modelId))
                {
                    modelPath = file;
                    break;
                }
            }

            if(!string.IsNullOrEmpty(modelPath))
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
    }
}
