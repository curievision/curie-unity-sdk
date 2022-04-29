using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Linq;
using CurieCatalogExample;
using CurieSDK;

public class CurieCatalogDemo : MonoBehaviour
{
    private Curie _api;
    [SerializeField] private List<string> _modelIds;
    private bool Loading = false;

    public Transform ModelHolder;
    [SerializeField] private GameObject _curSpawnedObj;
    [SerializeField] private CurieModelViewerUI _curUI;
    [SerializeField] private MouseOrbitImproved _mouseOrbiter;
    [SerializeField] private MobileMaxCamera _mobileCamera;
    [SerializeField] private float _switchFloat = 0.7f;
    [SerializeField] private GameObject _scene;
    [SerializeField] private GridLayoutGroup _catalogHolder;
    [SerializeField] private CanvasScaler _canvasScaler;
    [SerializeField] private float _modelWidth = 170;
    
    private void Awake()
    {
        //Limit FPS to 30 for demo
        QualitySettings.vSyncCount = 0;

        // Add event handler for OnInitialize (must be in awake)
        Curie.OnInitialized += OnCurieInit;
    }

    private void Start()
    {
        Application.targetFrameRate = 30;

        _scene.SetActive(false);

#if UNITY_ANDROID || UNITY_IPHONE
        _mobileCamera.enabled = true;
        _mouseOrbiter.enabled = false;
#else
        _mobileCamera.enabled = false;
        _mouseOrbiter.enabled = true;
#endif

        _curUI._catalogPanel.SetActive(false);
        _curUI._catalogButton.gameObject.SetActive(false);
        _curUI._backButton.onClick.AddListener(ToggleCatalogPanel);
        _curUI._backButtonBg.onClick.AddListener(ToggleCatalogPanel);
        _curUI._backButtonBg.gameObject.SetActive(false);
        _curUI._catalogButton.onClick.AddListener(ToggleCatalogPanel);
        _curUI._searchButton.onClick.AddListener(() => SearchProducts(false));
        _curUI.ProductName.text = _curUI.ProductDesc.text = "";
    }

    private void OnCurieInit(object sender, EventArgs e)
    {
        Debug.Log("[Curie Demo] Curie initialized, loading demo");
        _api = Curie.Instance;
        SearchProducts(true);
    }

    private void Update()
    {
        var curScale = Screen.width / _canvasScaler.referenceResolution.x;

        _catalogHolder.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _catalogHolder.constraintCount = Math.Max(2, (int)Math.Floor(Screen.width / _modelWidth));



#if UNITY_ANDROID || UNITY_IPHONE
        _catalogHolder.constraintCount = 2;
#endif

        var x = Screen.width;
        var y = Screen.height;
        var aspectRatio = AspectRatio(x, y);
        _canvasScaler.matchWidthOrHeight = aspectRatio == "16:9" ? 1 : 0;

        return;
    }

    public string AspectRatio(int x, int y)
    {
        double value = (double)x / y;
        if (value > _switchFloat)
            return "16:9";
        else
            return "9:16";
    }

    private void SearchProducts(bool loadFirst)
    {
        StartCoroutine(SearchProductCR(loadFirst));
    }

    private IEnumerator SearchProductCR(bool loadFirst)
    {
        if (Loading) yield break;

        Loading = true;
        _curUI._loadingPanel.SetActive(true);
        
        foreach (var b in _curUI._modelButtons)
        {
            b.gameObject.SetActive(false);
        }

        var search = _curUI._searchField.text;
        var searchRoutine = _api.SearchProducts(search);
        yield return searchRoutine.Coroutine;
        var products = searchRoutine.GetResult();

        _modelIds = products.Select(p => p.id).ToList();
        for (int i = 0; i < _modelIds.Count; i++)
        {
            StartCoroutine(LoadThumbnail(i, loadFirst));
        }

        for (int i = 0; i < _curUI._modelButtons.Count; i++)
        {
            CurieCatalogEntry b = _curUI._modelButtons[i];

            if (i < _modelIds.Count)
                b.gameObject.SetActive(true);
        }

        Loading = false;
        _curUI._loadingPanel.SetActive(false);
    }

    private void ToggleCatalogPanel()
    {

        var toggleIn = !_curUI._catalogPanel.activeInHierarchy;
        if(toggleIn)
        {
            _curUI.TweenPanel();
            _curUI._catalogPanel.SetActive(!_curUI._catalogPanel.activeInHierarchy);
        }
        else
        {
            _curUI.TweenPanelOut();
        }
    }

    /// <summary>
    /// Loads a model by Model ID with a temporary reference id to access the instantiated object
    /// </summary>
    private IEnumerator TestLoadModels()
    {
        for (int i = 0; i < _modelIds.Count; i++)
        {
            StartCoroutine(LoadThumbnail(i, true));
        }

        yield return null;
    }

    private IEnumerator LoadThumbnail(int i, bool showFirst)
    {
        string model = _modelIds[i];

        // Get the product media data
        var productDataCall = _api.GetProductData(model);
        yield return productDataCall.Coroutine;

        // Get thumbnail image
        var productData = productDataCall.GetResult();
        Debug.Log("n:" + productData.name);
        var modelThumbnail = productData.thumbnail_url;

        // Get detailed info

        if (string.IsNullOrEmpty(modelThumbnail))
        {
            Debug.LogError("No available thumbnail image for this model: " + model);
        }

        yield return SetImage(_curUI._modelButtons[i].Image, modelThumbnail);

        _curUI._modelButtons[i].ProductName.text = productData.name;
        _curUI._modelButtons[i].ProductBrand.text = productData.brand;
        _curUI._modelButtons[i].Button.onClick.RemoveAllListeners();
        _curUI._modelButtons[i].Button.onClick.AddListener(() => OnButtonClick(model));

        if(i == 2 && showFirst)
        {
            OnButtonClick(model);
        }

        Debug.Log("[Curie] Loaded thumbnail for ID: " + model);
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
            var t = new Texture2D(0,0);
            t.LoadImage(resultBytes);
            Sprite sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f));
            image.sprite = sprite;
        }
    }

    /// <summary>
    /// Handler for button clicks to load the model file and instantiate it
    /// </summary>
    private void OnButtonClick(string modelId)
    {
        StartCoroutine(ButtonClick(modelId));
    }

    private IEnumerator ButtonClick(string modelId)
    {
        if (Loading)
            yield break;    
        Loading = true;
        _curUI.ProductName.text = "";
        _curUI.ProductDesc.text = "";
        _curUI.PolyCount.text = "";
        _curUI.VertCount.text = "";

        var panelVisible = _curUI._catalogPanel.activeInHierarchy;
        if (panelVisible)
        {
            _curUI.TweenPanelOut();
        }

        _curUI._polyCountHolder.SetActive(false);
        _curUI._loadingPanel.SetActive(true);

        if (_curSpawnedObj)
        {
            Destroy(_curSpawnedObj);
            _curSpawnedObj = null;
        }

        var instantiateCall = _api.InstantiateModel(modelId);
        yield return instantiateCall.Coroutine;

        var productDataCall = _api.GetProductData(modelId);
        yield return productDataCall.Coroutine;
        var productData = productDataCall.GetResult();

        _curUI._polyCountHolder.SetActive(true);
        _curUI._loadingPanel.SetActive(false);

        var modelObject = instantiateCall.GetResult();
        modelObject.transform.parent = ModelHolder;
        modelObject.transform.localPosition = Vector3.zero;
        _mouseOrbiter.ObjectToScale = modelObject;

        _curSpawnedObj = modelObject;
        Debug.Log("[Curie] Instantiated: " + modelObject.name);

        var modelInfo = modelObject.GetComponent<CurieModelTracker>();

        _curUI.ProductName.text = productData.name;
        _curUI.ProductDesc.text = productData.description;
        
        _curUI.ProductNameDev.text = "<b>Model Name:</b> " + productData.name;
        _curUI.ProductDescDev.text = "<b>Model Description:</b> " + productData.description;
        
        _curUI.PolyCount.text = "<b>Poly Count:</b> " + CurieModelTools.CalcPolyCount(modelObject.transform, 0).ToString();
        _curUI.VertCount.text = "<b>Vert Count:</b> " + CurieModelTools.CalcVerticies(modelObject.transform, 0).ToString();
        _curUI.FileSize.text = "<b>File Size (Mb):</b> " + (((float)modelInfo.FileSize / 1024 / 1024)).ToString("F2");

        _scene.SetActive(true);

        _curUI._catalogButton.gameObject.SetActive(true);
        _curUI._backButtonBg.gameObject.SetActive(false);

        if(!_curUI.InitialisedTweens)
            _curUI.SetupTweens();

        Loading = false;
    }
}