using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Siccity.GLTFUtility;
using UnityEngine.Networking;
using CurieSDK;
using CurieSDK;

public class GLTFLoader : MonoBehaviour
{
    public string filePath;
    public Transform spawnPos;
    public string url;

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(GetRequest(url));
            //ImportGLTF(filePath);
        }


        if (Input.GetKeyDown(KeyCode.F))
        {
            StartCoroutine(GetRequest(url));
            //ImportGLTF(filePath);
        }
    }

    void ImportGLTF(string filepath)    
    {
        GameObject result = Importer.LoadFromFile(filepath);
        result.transform.parent = spawnPos;
        result.transform.localPosition = Vector3.zero;
    }

    IEnumerator GetRequest(string uri)
    {
        var resultUrl = "";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            webRequest.SetRequestHeader("x-curie-api-key", "dtga6wp1t5bPHDC_nkMqzOb2ljwNeD2h");
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    var resultJson = webRequest.downloadHandler.text;
                    var resultObj = JsonUtility.FromJson<ModelResultHolder>("{\"media\":" + resultJson + "}");
                    resultUrl = resultObj.media[0].url;
                    //Debug.Log();
                    Debug.Log(resultJson);
                    break;
            }
        }
        Debug.Log(resultUrl);
        using (UnityWebRequest webRequest = UnityWebRequest.Get(resultUrl))
        {
            yield return webRequest.SendWebRequest();
            var resultBytes = webRequest.downloadHandler.data;
            Debug.Log(resultBytes.Length);
            var gameObj = Importer.LoadFromBytes(resultBytes);
            gameObj.transform.localPosition = Vector3.zero;
            foreach(Transform child in gameObj.transform)
            {
                child.localPosition = Vector3.zero;
            }
            gameObj.name = gameObj.name + "_LOADED_FROM_API";
        }
    }
}
