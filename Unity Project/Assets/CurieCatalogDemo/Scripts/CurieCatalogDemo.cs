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
    public Transform ModelHolder;
    [SerializeField] private GameObject _curSpawnedObj;
    [SerializeField] private CurieModelViewerUI _curUI;
    [SerializeField] private List<CurieModelViewerUI> _UIViews;
    [SerializeField] private MouseOrbitImproved _mouseOrbiter;
    [SerializeField] private MobileMaxCamera _mobileCamera;
    [SerializeField] private float _switchFloat = 0.7f;
    [SerializeField] private GameObject _scene;
    [SerializeField] private GridLayoutGroup _catalogHolder;
    [SerializeField] private CanvasScaler _canvasScaler;
    [SerializeField] private float _modelWidth = 170;

    private bool Loading = false;
    private void Start()
    {
        //Limit FPS to 30 for demo
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

        _scene.SetActive(false);

        Curie.OnInitialized += OnCurieInit;

#if UNITY_ANDROID || UNITY_IPHONE
        _mobileCamera.enabled = true;
        _mouseOrbiter.enabled = false;
#else
        _mobileCamera.enabled = false;
        _mouseOrbiter.enabled = true;
#endif



        _UIViews.ForEach(u =>
        {
            u._catalogPanel.SetActive(false);
            u._catalogButton.gameObject.SetActive(false);
            u._backButton.onClick.AddListener(ToggleCatalogPanel);
            u._backButtonBg.onClick.AddListener(ToggleCatalogPanel);
            u._backButtonBg.gameObject.SetActive(false);
            u._catalogButton.onClick.AddListener(ToggleCatalogPanel);
            u._searchButton.onClick.AddListener(() => SearchProducts(false));
            u.ProductName.text = u.ProductDesc.text = "";
        });
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
        //Debug.Log("Width:" + Screen.width);
        //Debug.Log("Scale:" + curScale);

        _catalogHolder.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _catalogHolder.constraintCount = Math.Max(2, (int)Math.Floor(Screen.width / _modelWidth));

        var x = Screen.width;
        var y = Screen.height;
        var aspectRatio = AspectRatio(x, y);
        _canvasScaler.matchWidthOrHeight = aspectRatio == "16:9" ? 1 : 0;

        return;

        //Debug.Log(string.Format("{0}x{1} ({2})", Screen.width, Screen.height));  

        //Debug.Log(aspectRatio);


        _curUI = aspectRatio == "16:9" ? _UIViews[0] : _UIViews[1];
        var inactiveUI = aspectRatio == "16:9" ? _UIViews[1] : _UIViews[0];
        inactiveUI.gameObject.SetActive(false);
        if (!_curUI.gameObject.activeInHierarchy)
        {
            _curUI.gameObject.SetActive(true);
        }
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
        _UIViews.ForEach( u => u._loadingPanel.SetActive(true));
        _UIViews.ForEach(u =>
        {
            foreach (var b in u._modelButtons)
            {
                b.gameObject.SetActive(false);
            }
        });

        var search = _curUI._searchField.text;
        var searchRoutine = _api.SearchProducts(search);
        yield return searchRoutine.Coroutine;
        var products = searchRoutine.GetResult();

        _modelIds = products.Select(p => p.id).ToList();
        for (int i = 0; i < _modelIds.Count; i++)
        {
            StartCoroutine(LoadThumbnail(i, loadFirst));
        }

        //model count = 1
        //mod = 0
        //i = 0 ;
        _UIViews.ForEach(u =>
        {
            for (int i = 0; i < u._modelButtons.Count; i++)
            {
                CurieCatalogEntry b = u._modelButtons[i];

                if (i < _modelIds.Count)
                    b.gameObject.SetActive(true);
            }
        });

        Loading = false;
        _UIViews.ForEach(u =>
        {
            u._loadingPanel.SetActive(false);
        });
    }

    private void ToggleCatalogPanel()
    {
        _UIViews.ForEach(u =>
        {
            var toggleIn = !u._catalogPanel.activeInHierarchy;
            if(toggleIn)
            {
                u.TweenPanel();
                u._catalogPanel.SetActive(!u._catalogPanel.activeInHierarchy);
            }
            else
            {
                u.TweenPanelOut();
            }
        });
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
        var modelThumbnail = productData.thumbnail_url;

        // Get detailed info

        if (string.IsNullOrEmpty(modelThumbnail))
        {
            Debug.LogError("No available thumbnail image for this model: " + model);
        }

        foreach(var u in _UIViews)
        {
            yield return SetImage(u._modelButtons[i].Image, modelThumbnail);
        }

        _UIViews.ForEach(u =>
        {
            u._modelButtons[i].ProductName.text = productData.name;
            u._modelButtons[i].ProductBrand.text = productData.brand;
            u._modelButtons[i].Button.onClick.RemoveAllListeners();
            u._modelButtons[i].Button.onClick.AddListener(() => OnButtonClick(model));
        });

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
        _UIViews.ForEach(u =>
        {
            var panelVisible = u._catalogPanel.activeInHierarchy;
            if (panelVisible)
            {
                u.TweenPanelOut();
            }
        });
        _UIViews.ForEach(u =>
        {
            u._polyCountHolder.SetActive(false);
            u._loadingPanel.SetActive(true);
        });
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
        _UIViews.ForEach(u =>
        {
            u._polyCountHolder.SetActive(true);
            u._loadingPanel.SetActive(false);
        });
        var modelObject = instantiateCall.GetResult();
        modelObject.transform.parent = ModelHolder;
        modelObject.transform.localPosition = Vector3.zero;
        _mouseOrbiter.ObjectToScale = modelObject;

        _curSpawnedObj = modelObject;
        Debug.Log("[Curie] Instantiated: " + modelObject.name);
        _UIViews.ForEach(_u =>
        {
            var modelInfo = modelObject.GetComponent<CurieModelTracker>();

            _u.ProductName.text = productData.name;
            _u.ProductDesc.text = productData.description;

            _u.ProductNameDev.text = "<b>Model Name:</b> " + productData.name;
            _u.ProductDescDev.text = "<b>Model Description:</b> " + productData.description;

            _u.PolyCount.text = "<b>Poly Count:</b> " + CurieModelFix.CalcPolyCount(modelObject.transform, 0).ToString();
            _u.VertCount.text = "<b>Vert Count:</b> " + CurieModelFix.CalcVerticies(modelObject.transform, 0).ToString();
            _u.FileSize.text = "<b>File Size (Mb):</b> " + (((float)modelInfo.FileSize / 1024 / 1024)).ToString("F2");
        });



        //_mouseOrbiter.target.transform.position = modelObject.transform.position;
        //var p = _mouseOrbiter.target.transform.position;
        //p.y = CurieModelFix.CalcMidPoint(modelObject.transform);
        //_mouseOrbiter.target.transform.position = p ;


        //_mouseOrbiter.target.parent = modelObject.transform;
        //Debug.Log("SIZE:: " + CurieModelFix.CalcSize(modelObject.transform, Vector3.zero));

        _scene.SetActive(true);
        _UIViews.ForEach(u =>
        {
            u._catalogButton.gameObject.SetActive(true);
            u._backButtonBg.gameObject.SetActive(false);
        });

        _UIViews.ForEach(u =>
        {
            if(!u.InitialisedTweens)
                u.SetupTweens();
        });

        Loading = false;
    }
}