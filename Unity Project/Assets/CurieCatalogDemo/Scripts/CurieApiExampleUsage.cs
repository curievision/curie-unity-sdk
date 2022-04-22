using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Siccity.GLTFUtility;
using UnityEngine.Networking;
using CurieSDK;
using System;
using System.Linq;
using DG.Tweening;

public class CurieApiExampleUsage : MonoBehaviour
{
    [SerializeField] private CurieApiExample _api;
    [SerializeField] private List<string> _modelIds;
    [SerializeField] private List<GameObject> _spawnedModels;
    public bool DoFallingDemo;
    public bool DoStarfieldDemo;
    public GameObject StarfieldTrail;
    public bool StarfieldSide;

    private void Start()
    {
        Physics.gravity = new Vector3(0, -5.0F, 0);
        StartCoroutine(TestLoadModels());
    }

    /// <summary>
    /// Loads a model by Model ID with a temporary reference id to access the instantiated object
    /// </summary>
    private IEnumerator TestLoadModels()
    {
        foreach(var model in _modelIds)
        {
            var refId = Guid.NewGuid().ToString();
            yield return _api.InstantiateModel(model, refId);
            Debug.Log("Object spawned");
            var modelObject = _api.GetSpawnedModel(refId);
            modelObject.SetActive(false);

            _spawnedModels.Add(modelObject);

            if (DoFallingDemo)
            {
                var tList = GetAllTransforms(modelObject.transform);


                foreach (var t in tList)
                {
                    var r = t.GetComponent<MeshRenderer>();
                    if (r)
                    {
                        var m = r.transform.gameObject.AddComponent<MeshCollider>();
                        m.convex = true;
                    }
                }
                var rb = modelObject.AddComponent<Rigidbody>();
                rb.mass = 10;

                StartCoroutine(FallingObjCR(modelObject));
            }

            if (DoStarfieldDemo)
            {
                
                var trail = GameObject.Instantiate(StarfieldTrail);
                trail.transform.parent = modelObject.transform;
                trail.transform.localPosition = Vector3.zero;

                modelObject.SetActive(true);
                var pos = new[] { UnityEngine.Random.Range(-6, -1), UnityEngine.Random.Range(1, 6) }[StarfieldSide ? 0 : 1];
                StarfieldSide = !StarfieldSide;

                modelObject.transform.position = new Vector3(pos, UnityEngine.Random.Range(0.2f, 4), -20);


                var seq = DOTween.Sequence();
                seq.Append(modelObject.transform.DOMoveZ(modelObject.transform.position.z + 30, 1.3f).From(-20));
                seq.Join(modelObject.transform.DOShakeRotation(3f, 25,5, 10));
                seq.Join(modelObject.transform.DOPunchRotation(new Vector3(20, 20, 20), 3f));
                seq.SetLoops(-1);
                seq.AppendInterval(0.3f);
                seq.Play();
                yield return new WaitForSeconds(0.3f);
            }

            Debug.Log("[Curie] Instantiated: " + modelObject.name);
        }



        //if (DoStarfieldDemo)
        //{
        //    foreach(var m in _spawnedModels)
        //    {
        //        m.SetActive(true);
        //        var pos = new[] { UnityEngine.Random.Range(-6, -1), UnityEngine.Random.Range(1, 6) }[UnityEngine.Random.Range(0, 1 + 1)];
        //        m.transform.position = new Vector3(pos, UnityEngine.Random.Range(0.2f, 4), -20);
        //
        //        var seq = DOTween.Sequence();
        //        seq.Append(m.transform.DOMoveZ(m.transform.position.z + 30, 1f).From(-20));
        //        seq.SetLoops(-1);
        //        seq.AppendInterval(0.3f);
        //        seq.Play();
        //        yield return new WaitForSeconds(0.3f);
        //    }
        //}


    }

    private IEnumerator FallingObjCR(GameObject g)
    {
        g.transform.position = new Vector3(0, 20, 0);
        yield return new WaitForSeconds(0.1f);
        g.SetActive(true);
        yield return new WaitForSeconds(20f);

        if(_spawnedModels.Count > 50)
        {
            var s = _spawnedModels[0];
            _spawnedModels.RemoveAt(0);
            Destroy(s.gameObject);
        }

        var cloneObj = Instantiate(g);
        _spawnedModels.Add(cloneObj);
        StartCoroutine(FallingObjCR(cloneObj));
    }


    static List<Transform> GetAllTransforms(Transform parent)
    {
        var transformList = new List<Transform>();
        BuildTransformList(transformList, parent);
        return transformList;
    }

    private static void BuildTransformList(ICollection<Transform> transforms, Transform parent)
    {
        if (parent == null) { return; }
        foreach (Transform t in parent)
        {
            transforms.Add(t);
            BuildTransformList(transforms, t);
        }
    }
}
