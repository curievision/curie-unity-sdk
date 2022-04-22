using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using CurieSDK;

namespace CurieCatalogExample
{
    public class CurieModelViewerUI : MonoBehaviour
    {
        public Curie API;
        public TextMeshProUGUI ProductName;
        public TextMeshProUGUI ProductDesc;

        public TextMeshProUGUI ProductNameDev;
        public TextMeshProUGUI ProductDescDev;  

        public TextMeshProUGUI PolyCount;
        public TextMeshProUGUI VertCount;
        public TextMeshProUGUI FileSize;
        public TextMeshProUGUI LastDevOutput;


        [SerializeField] public List<CurieCatalogEntry> _modelButtons;
        [SerializeField] public Button _backButton;
        [SerializeField] public Button _backButtonBg;
        [SerializeField] public Button _catalogButton;
        
        [SerializeField] public Button _devToolsButton;
        [SerializeField] public GameObject _devToolsHolder;

        [SerializeField] public GameObject _lowerPanel;

        [SerializeField] public Button _searchButton;
        [SerializeField] public TMP_InputField _searchField;

        [SerializeField] public GameObject _catalogPanel;
        [SerializeField] public GameObject _catalogHolder;
        [SerializeField] public GameObject _loadingPanel;
        [SerializeField] public GameObject _polyCountHolder;


        [SerializeField] public RectTransform _catalogPanelStart;
        [SerializeField] public RectTransform _catalogPanelEnd;


        [SerializeField] public GameObject _descPanel;
        [SerializeField] public RectTransform _descPanelStart;
        [SerializeField] public RectTransform _descPanelEnd;

        private Tween _curTween;
        private Tween _curTween2;

        public bool InitialisedTweens;

        public CanvasGroup PanTweenObj;
        public CanvasGroup ZoomTweenObj;
        public CanvasGroup MoveTweenObj;
        public CanvasGroup ZoomTweenObjAMobile;
        public CanvasGroup ZoomTweenObjBMobile;
        public CanvasGroup ZoomTweenObjCMobile;
        public CanvasGroup MoveTweenObjMobile;

        public Vector2 CachedPanStartPos;
        public Vector2 CachedZoomStartPos;

        public Vector2 CachedZoomStartPosA;
        public Vector2 CachedZoomStartPosB;

        public Vector2 CachedMoveStartPos;

        public Sequence PanTween;
        public Sequence ZoomTween;
        public Sequence ZoomTweenMobile;
        public Sequence MoveTween;
        public Sequence MoveTweenMobile;

        public bool PanTweenActive;
        public bool ZoomTweenActive;
        public bool ZoomTweenActiveMobile;
        public bool MoveTweenActive;
        public bool MoveTweenActiveMobile;


        private void Start()
        {
            API = null;
            Curie.OnInitialized += OnCurieInit;

            DOTween.defaultAutoPlay = AutoPlay.None;

            _lowerPanel.gameObject.SetActive(true);
            _devToolsHolder.gameObject.SetActive(false); 

            _catalogPanel.GetComponent<RectTransform>().anchoredPosition = _catalogPanelStart.anchoredPosition;
            _descPanel.GetComponent<RectTransform>().anchoredPosition = _descPanelEnd.anchoredPosition;
            _polyCountHolder.SetActive(false);

            _devToolsButton.onClick.AddListener(() =>
            {
                _devToolsHolder.SetActive(!_devToolsHolder.activeInHierarchy);
            });

            PanTweenObj.gameObject.SetActive(false);
            ZoomTweenObj.gameObject.SetActive(false);
            MoveTweenObj.gameObject.SetActive(false);
            ZoomTweenObjAMobile.gameObject.SetActive(false);
            ZoomTweenObjBMobile.gameObject.SetActive(false);
            ZoomTweenObjCMobile.gameObject.SetActive(false);
            MoveTweenObjMobile.gameObject.SetActive(false);

        }

        private void OnCurieInit(object sender, EventArgs e)
        {
            API = Curie.Instance;
        }

        private bool IsMobile()
        {
#if UNITY_ANDROID || UNITY_IPHONE
            return true;
#else
            return false;
#endif

        }

        public void SetupTweens()
        {

            if (InitialisedTweens) return;

            InitialisedTweens = true;
            PanTweenActive = true;
            ZoomTweenActive = true;
            MoveTweenActive = true;

            // Pan
            CachedPanStartPos = PanTweenObj.GetComponent<RectTransform>().anchoredPosition; 
            PanTween = DOTween.Sequence()
                .AppendInterval(8.0f)
                .AppendCallback(() => {
                    PanTweenObj.gameObject.SetActive(true);
                    PanTweenObj.GetComponent<RectTransform>().anchoredPosition = CachedPanStartPos; 
                })
                .Append(PanTweenObj.DOFade(1, 0.5F))
                .Join(PanTweenObj.GetComponent<RectTransform>().DOAnchorPosX(CachedPanStartPos.x + 200, 1.0f).SetEase(Ease.InOutSine))
                .Append(PanTweenObj.DOFade(0, 0.5F))
                .AppendCallback(() => PanTweenObj.gameObject.SetActive(false))
                .Play()
                .OnComplete(() => PanTweenActive = false);

            // Move
            var moveObj = IsMobile() ? MoveTweenObjMobile : MoveTweenObj;
            CachedMoveStartPos = moveObj.GetComponent<RectTransform>().anchoredPosition;

            MoveTween = DOTween.Sequence()
                .AppendInterval(17.0f)
                .AppendCallback(() => {
                    moveObj.gameObject.SetActive(true);
                    moveObj.GetComponent<RectTransform>().anchoredPosition = CachedMoveStartPos;
                })
                .Append(moveObj.DOFade(1, 0.5F))
                .Join(moveObj.GetComponent<RectTransform>().DOAnchorPosY(CachedMoveStartPos.y + 200, 0.7f).SetEase(Ease.InOutSine))
                .Append(moveObj.GetComponent<RectTransform>().DOAnchorPosX(CachedMoveStartPos.x + 200, 0.7f).SetEase(Ease.InOutSine))
                .Append(moveObj.DOFade(0, 0.5F))
                .AppendCallback(() => moveObj.gameObject.SetActive(false))
                .OnComplete(() => MoveTweenActive = false);

            MoveTweenMobile = DOTween.Sequence()
                .AppendInterval(17.0f)
                .AppendCallback(() => {
                    moveObj.gameObject.SetActive(true);
                    moveObj.GetComponent<RectTransform>().anchoredPosition = CachedMoveStartPos;
                })
                .Append(moveObj.DOFade(1, 0.5F))
                .Join(moveObj.GetComponent<RectTransform>().DOAnchorPosY(CachedMoveStartPos.y + 200, 0.7f).SetEase(Ease.InOutSine))
                .Append(moveObj.GetComponent<RectTransform>().DOAnchorPosX(CachedMoveStartPos.x + 200, 0.7f).SetEase(Ease.InOutSine))
                .Append(moveObj.DOFade(0, 0.5F))
                .AppendCallback(() => moveObj.gameObject.SetActive(false))
                .OnComplete(() => MoveTweenActiveMobile = false);

            if(IsMobile())
            {
                MoveTweenMobile.Play();
            }
            else
            {
                MoveTween.Play();
            }

            // Zoom
            CachedZoomStartPos = ZoomTweenObj.GetComponent<RectTransform>().anchoredPosition;

            ZoomTween = DOTween.Sequence()
                .AppendInterval(26.0f)
                .AppendCallback(() => {
                    ZoomTweenObj.gameObject.SetActive(true);
                    ZoomTweenObj.GetComponent<RectTransform>().anchoredPosition = CachedPanStartPos;
                })
                .Append(ZoomTweenObj.DOFade(1, 0.5F))
                .Join(ZoomTweenObj.GetComponent<RectTransform>().DOAnchorPosY(CachedPanStartPos.y + 100, 1.0f).SetEase(Ease.InOutSine))
                .Append(ZoomTweenObj.GetComponent<RectTransform>().DOAnchorPosY(CachedPanStartPos.y - 100, 1.0f).SetEase(Ease.InOutSine))
                .Append(ZoomTweenObj.DOFade(0, 0.5F))
                .AppendCallback(() => ZoomTweenObj.gameObject.SetActive(false))
                .OnComplete(() => ZoomTweenActive = false);

            CachedZoomStartPosA = ZoomTweenObjAMobile.GetComponent<RectTransform>().anchoredPosition;
            CachedZoomStartPosB = ZoomTweenObjBMobile.GetComponent<RectTransform>().anchoredPosition;

            ZoomTweenMobile = DOTween.Sequence()
                .AppendInterval(26.0f)
                .AppendCallback(() => {
                    ZoomTweenObjAMobile.gameObject.SetActive(true);
                    ZoomTweenObjAMobile.GetComponent<RectTransform>().anchoredPosition = CachedZoomStartPosA;

                    ZoomTweenObjBMobile.gameObject.SetActive(true);
                    ZoomTweenObjBMobile.GetComponent<RectTransform>().anchoredPosition = CachedZoomStartPosB;


                    ZoomTweenObjCMobile.gameObject.SetActive(true);

                })
                .Append(ZoomTweenObjAMobile.DOFade(1, 0.5F))
                .Join(ZoomTweenObjBMobile.DOFade(1, 0.5F))
                .Join(ZoomTweenObjCMobile.DOFade(1, 0.5f))
                .AppendInterval(0.5F)
                .Append(ZoomTweenObjAMobile.GetComponent<RectTransform>()
                    .DOAnchorPos(new Vector2(CachedZoomStartPosA.x - 120, CachedZoomStartPosA.y - 120), 1.0f)
                    .SetEase(Ease.InOutSine)
                )
                .Join(ZoomTweenObjBMobile.GetComponent<RectTransform>()
                    .DOAnchorPos(new Vector2(CachedZoomStartPosB.x + 120, CachedZoomStartPosB.y + 120), 1.0f)
                    .SetEase(Ease.InOutSine)
                )
                .AppendInterval(0.5f)
                .Append(ZoomTweenObjAMobile.DOFade(0, 0.5F))
                .Join(ZoomTweenObjBMobile.DOFade(0, 0.5F))
                .Join(ZoomTweenObjCMobile.DOFade(0, 0.5f))
                .AppendCallback(() => ZoomTweenObjAMobile.gameObject.SetActive(false))
                .AppendCallback(() => ZoomTweenObjBMobile.gameObject.SetActive(false))
                .AppendCallback(() => ZoomTweenObjCMobile.gameObject.SetActive(false))
                .OnComplete(() => ZoomTweenActiveMobile = false);

            if (IsMobile())
            {
                ZoomTweenMobile.Play();
            }
            else
            {
                ZoomTween.Play();
            }
        }

        public void HideTween(string tweenName)
        {
            return;

            switch(tweenName)
            {
                case "Pan":
                    PanTween.Kill(false);
                    PanTweenActive = false;
                    break;
                case "Zoom":
                    if(SystemInfo.deviceType == DeviceType.Handheld)
                    {
                        ZoomTweenMobile.Kill(false);
                        ZoomTweenActiveMobile = false;
                    }
                    else
                    {
                        ZoomTween.Kill(false);
                        ZoomTweenActive = false;
                    }
                    
                    break;
                case "Move":
                    if (SystemInfo.deviceType == DeviceType.Handheld)
                    {
                        MoveTweenMobile.Kill(false);
                        MoveTweenActiveMobile = false;
                    }
                    else
                    {
                        MoveTween.Kill(false);
                        MoveTweenActive = false;
                    }
                    break;
            }
        }

        private void Update()
        {
            if (API == null) return;

            LastDevOutput.text = "<b>Last API Request:</b> \n---\n" + API.LastDevCall + "\n---\n" +
                                "<b>Last API Response:</b> \n---\n" + API.LastDevOutput;
        }


        public void TweenPanel()
        {
            if (_curTween != null)
            {
                _curTween.Kill(false);
            }

            _curTween = _catalogPanel.GetComponent<RectTransform>()
                .DOAnchorPos(_catalogPanelEnd.anchoredPosition, 0.75f)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    _backButtonBg.gameObject.SetActive(true);
                })
                .Play();


            if (_curTween2 != null)
            {
                _curTween2.Kill(false);
            }

            _polyCountHolder.SetActive(false);
            _curTween2 = _descPanel.GetComponent<RectTransform>()
                .DOAnchorPos(_descPanelStart.anchoredPosition, 0.75f)
                .SetEase(Ease.InOutSine).Play();
        }
        public void TweenPanelOut()
        {
            _backButtonBg.gameObject.SetActive(false);
            if (_curTween != null)
            {
                _curTween.Kill(false);
            }
            _curTween = _catalogPanel.GetComponent<RectTransform>()
                .DOAnchorPos(_catalogPanelStart.anchoredPosition, 0.75f)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    _catalogPanel.SetActive(false);
                })
                .Play();


            if (_curTween2 != null)
            {
                _curTween2.Kill(false);
            }
            _polyCountHolder.SetActive(true);
            _curTween2 = _descPanel.GetComponent<RectTransform>()
                .DOAnchorPos(_descPanelEnd.anchoredPosition, 0.75f)
                .SetEase(Ease.InOutSine).Play();
        }

    }
}
