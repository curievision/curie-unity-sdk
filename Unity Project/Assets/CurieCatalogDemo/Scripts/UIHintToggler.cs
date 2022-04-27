using UnityEngine;
using UnityEngine.UI;

public class UIHintToggler : MonoBehaviour
{
    public Image Image;
    public Sprite MobileImage;
    public Sprite DesktopImage;

    void Awake()
    {

#if UNITY_ANDROID || UNITY_IPHONE
        Image.sprite = MobileImage;
#else
        Image.sprite = DesktopImage;
#endif
    }
}