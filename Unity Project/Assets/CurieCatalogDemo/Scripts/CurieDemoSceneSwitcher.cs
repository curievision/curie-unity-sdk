using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CurieDemoSceneSwitcher : MonoBehaviour
{
    public Button ShowButtons;
    public GameObject ButtonPanel;
    public List<Button> Buttons;

    void Start()
    {
        ShowButtons.onClick.RemoveAllListeners();
        ShowButtons.onClick.AddListener(() =>
        {
            ButtonPanel.SetActive(!ButtonPanel.activeInHierarchy);
        });

        for (int i = 0; i < Buttons.Count; i++)
        {
            Button b = Buttons[i];
            b.onClick.RemoveAllListeners();
            var buttonRef = i;
            b.onClick.AddListener(() => SceneManager.LoadScene("Demo_" + buttonRef));

        }
    }
}
