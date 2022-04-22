using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Siccity.GLTFUtility;
using UnityEngine.Networking;
using CurieSDK;
using System;
using System.Linq;
using CurieSDK;

public class CurieApiV1Test : MonoBehaviour
{
    public float waitTime = 0.2f;
    public List<string> ModelsToSpawn;
    public string ClientId;
    public string ApiKey;
    public bool _bearerCached;
    public string _bearerToken;
    public bool ApplyFix;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(InstantiateModel());
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            StartCoroutine(LoadProducts());
        }
    }

    void ImportGLTF(string filepath)    
    {
        GameObject result = Importer.LoadFromFile(filepath);
        //result.transform.parent = spawnPos;
        result.transform.localPosition = Vector3.zero;
    }

    IEnumerator LoadProducts()
    {

        if (!_bearerCached)
        {
            Debug.Log("Bearer not cached, getting bearer token");
            yield return StartCoroutine(CacheBearerToken());
        }

        Debug.Log("Getting products");
        var api_url = string.Format("https://api.curie.io/admin/v1/products?skip=0&limit=100&sort=-created_on");
        var api_url2 = string.Format("https://api.curie.io/admin/v1/products?skip=100&limit=100&sort=-created_on");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(api_url))
        {

            var token = "Bearer " + _bearerToken;
            webRequest.SetRequestHeader("Authorization", token);
            webRequest.SetRequestHeader("accept", "application/json");
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    var resultJson = webRequest.downloadHandler.text;
                    //Debug.Log(resultJson);
                    var resultObj = JsonUtility.FromJson<ProductList>(resultJson);
                    foreach(var p in resultObj.products)
                    {
                        ModelsToSpawn.Add(p.id);
                    }
                    //Debug.Log();

                    break;
            }
        }

        using (UnityWebRequest webRequest = UnityWebRequest.Get(api_url2))
        {

            var token = "Bearer " + _bearerToken;
            webRequest.SetRequestHeader("Authorization", token);
            webRequest.SetRequestHeader("accept", "application/json");
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    var resultJson = webRequest.downloadHandler.text;
                    //Debug.Log(resultJson);
                    var resultObj = JsonUtility.FromJson<ProductList>(resultJson);
                    foreach (var p in resultObj.products)
                    {
                        ModelsToSpawn.Add(p.id);
                    }
                    //Debug.Log();

                    break;
            }
        }
    }

    IEnumerator InstantiateModel()
    {
        if (!_bearerCached)
        {
            Debug.Log("Bearer not cached, getting bearer token");
            yield return StartCoroutine(CacheBearerToken());
        }

        foreach(var model in ModelsToSpawn)
        {
            //Debug.Log("Instantiating model with ID: " + model);
            var resultUrl = "";
            var api_url = string.Format("https://api.curie.io/admin/v1/products/{0}/media", model);
            using (UnityWebRequest webRequest = UnityWebRequest.Get(api_url))
            {

                var token = "Bearer " + _bearerToken;
                webRequest.SetRequestHeader("Authorization", token);
                webRequest.SetRequestHeader("accept", "application/json");
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                switch (webRequest.result)
                {
                    case UnityWebRequest.Result.Success:
                        var resultJson = webRequest.downloadHandler.text;
                        //Debug.Log(resultJson);
                        var resultObj = JsonUtility.FromJson<ModelResultHolder>("{\"media\":" + resultJson + "}");
                        var resultModel = resultObj.media.FirstOrDefault(m => m.format == "glb");
                        if(resultModel == null)
                        {
                            Debug.LogError("No valid glb model type found for model with ID: " + model);
                            continue;
                        }
                        resultUrl = resultModel.url;
                        break;
                }
            }

            //Debug.Log(resultUrl);
            using (UnityWebRequest webRequest = UnityWebRequest.Get(resultUrl))
            {
                yield return webRequest.SendWebRequest();
                var resultBytes = webRequest.downloadHandler.data;
                //Debug.Log(resultBytes.Length);

                var waiting = true;
                GameObject result = null;
                Importer.ImportGLBAsync(resultBytes, new ImportSettings(), 
                    (a, anims) =>
                {
                    waiting = false;
                    result = a;
                },
                (a) => { 

                });


                while (waiting)
                {
                    yield return null;
                }

                if (ApplyFix)
                {
                    var modelObj = CurieModelFix.RepositionModel(result);
                    modelObj.name = "CurieModel_" + model;
                }

            }

            yield return new WaitForSeconds(waitTime);
        }

    }

    private IEnumerator CacheBearerToken()
    {
        yield return new WaitForSeconds(2);
        
        _bearerToken = "test_bearer";
        var api_auth = "https://api.curie.io/auth";

        var data = new Dictionary<string, string>()
        {
                {"grant_type", "client_credentials"},
                { "client_id", ClientId },
                { "client_secret", ApiKey},
        };

        using (UnityWebRequest webRequest = UnityWebRequest.Post(api_auth, data))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();
        
            switch (webRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    var resultJson = webRequest.downloadHandler.text;
                    var resultObj = JsonUtility.FromJson<CurieBearerToken>(resultJson);
                    var bearer = resultObj.access_token;
                    _bearerToken = bearer;
                    _bearerCached = true;
                    //Debug.Log("Bearer ID cached: " + _bearerToken);
                    break;
            }
        }
    }
}
