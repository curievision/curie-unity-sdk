using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Siccity.GLTFUtility;
using UnityEngine.Networking;
using CurieSDK;
using System;
using System.Linq;
using DG.Tweening;

public class CurieApiCodeSnippets : MonoBehaviour
{
    [SerializeField] private CurieApiExample _api;

    private IEnumerator LoadModelById()
    {
        // ID of model to spawn and a generated ref ID to get a reference to it later
        var modelId = "625832925877777d3dcb2a1e";
        var refId = Guid.NewGuid().ToString();

        // Single line to load the model via the API and Instantiate it
        yield return _api.InstantiateModel(modelId, refId);

        // Get a reference to the spawned object
        var modelObject = _api.GetSpawnedModel(refId);
    }
}
