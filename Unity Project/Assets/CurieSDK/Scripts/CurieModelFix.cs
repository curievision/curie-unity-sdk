using System;
using UnityEngine;

namespace CurieSDK
{
    public static class CurieModelFix
    {
        /// <summary>
        /// Reposition a model to 0,0,0 and align correctly with ground.
        /// </summary>
        /// <param name="model">The model gameobject to reposition</param>
        /// <returns>Returns the fixed gameObject</returns>
        public static GameObject RepositionModel(GameObject model)
        {
            return FixObject(model);
        }

        /// <summary>
        /// Repositions to 0,0,0, calculating the lowest point of an object to ensure it doesn't overlap through the ground
        /// </summary>
        /// <param name="gameObj"></param>
        /// <returns>Returns the fixed gameObject</returns>
        private static GameObject FixObject(GameObject gameObj)
        {
            var holder = new GameObject();
            holder.transform.position = Vector3.zero;
            gameObj.transform.parent = holder.transform;
            gameObj.transform.localPosition = Vector3.zero;
            holder.name = "CurieModel_" + gameObj.name;

            var lowestSet = false;
            var lowest = 0f;

            lowest = CalcLowestPoint(gameObj.transform, lowestSet, lowest);

            // Fix misplaces objects due to child positioning
            if (lowest > 0f)
            {
                gameObj.transform.localPosition = Vector3.zero;
                foreach (Transform child in gameObj.transform)
                {
                    child.localPosition = Vector3.zero;
                }

                // Recalc lowest
                lowest = CalcLowestPoint(gameObj.transform, lowestSet, lowest);
                lowest += 0.0428f;
            }

            // Reposition misaligned objects
            var absOfLowest = Math.Abs(lowest);
            gameObj.transform.localPosition = new Vector3(0, absOfLowest, 0);

            return holder;
        }

        /// <summary>
        /// Recursive function to get the lowest Y position of a model based including it's child objects
        /// </summary>
        /// <returns>The lowest Y point of the object</returns>
        private static float CalcLowestPoint(Transform parentObj, bool lowestSet, float curLowest)
        {
            foreach (Transform t in parentObj.transform)
            {
                var meshR = t.GetComponent<MeshRenderer>();
                if (meshR == null) continue;

                var lowestPoint = meshR.bounds.min.y;
                var highestPoint = meshR.bounds.max.y;

                if (!lowestSet)
                {
                    curLowest = lowestPoint;
                    lowestSet = true;
                }
                else if (lowestPoint < curLowest)
                {
                    curLowest = lowestPoint;
                }

                if (t.childCount > 0)
                    curLowest = CalcLowestPoint(t, lowestSet, curLowest);
            }

            return curLowest;
        }


        /// <summary>
        /// Recursive function to get the highest Y position of a model based including it's child objects
        /// </summary>
        /// <returns>The highest Y point of the object</returns>
        private static float CalcHighestPoint(Transform parentObj, bool highestSet, float curHighest)
        {
            foreach (Transform t in parentObj.transform)
            {
                var meshR = t.GetComponent<MeshRenderer>();
                if (meshR == null) continue;

                var highestPoint = meshR.bounds.max.y;

                if (!highestSet)
                {
                    curHighest = highestPoint;
                    highestSet = true;
                }
                else if (highestPoint > curHighest)
                {
                    curHighest = highestPoint;
                }

                if (t.childCount > 0)
                    curHighest = CalcLowestPoint(t, highestSet, curHighest);
            }

            return curHighest;
        }


        /// <summary>
        /// Recursive function to get the highest Y position of a model based including it's child objects
        /// </summary>
        /// <returns>The highest Y point of the object</returns>
        public static Vector3 CalcSize(Transform parentObj,Vector3 totalSize)
        {
            foreach (Transform t in parentObj.transform)
            {
                var meshR = t.GetComponentInChildren<MeshRenderer>();
                if (meshR == null) continue;

                var size = meshR.bounds.size;
                totalSize += size;

                if (t.childCount > 0)
                    totalSize += CalcSize(t, totalSize);
            }

            return totalSize;
        }


        /// <summary>
        /// Recursive function to get the highest Y position of a model based including it's child objects
        /// </summary>
        /// <returns>The highest Y point of the object</returns>
        public static int CalcPolyCount(Transform parentObj, int totalSize)
        {
            foreach (Transform t in parentObj.transform)
            {
                var meshR = t.GetComponentInChildren<MeshFilter>();
                if (meshR == null) continue;

                var size = meshR.mesh.triangles.Length / 3;
;
                totalSize += size;

                if (t.childCount > 0)
                    totalSize += CalcPolyCount(t, totalSize);
            }

            return totalSize;
        }

        /// <summary>
        /// Recursive function to get the highest Y position of a model based including it's child objects
        /// </summary>
        /// <returns>The highest Y point of the object</returns>
        public static int CalcVerticies(Transform parentObj, int totalSize)
        {
            foreach (Transform t in parentObj.transform)
            {
                var meshR = t.GetComponentInChildren<MeshFilter>();
                if (meshR == null) continue;

                var size = meshR.mesh.vertices.Length;
                ;
                totalSize += size;

                if (t.childCount > 0)
                    totalSize += CalcVerticies(t, totalSize);
            }

            return totalSize;
        }

        /// <summary>
        /// Recursive function to get the lowest Y position of a model based including it's child objects
        /// </summary>
        /// <returns>The lowest Y point of the object</returns>
        public static float CalcMidPoint(Transform parentObj)
        {
            var lowestSet = false;
            var lowest = 0f;
            lowest = CalcLowestPoint(parentObj.transform, lowestSet, lowest);


            var highestSet = false;
            var highest = 0f;
            highest = CalcHighestPoint(parentObj.transform, highestSet, highest);

            var midPoint = lowest + ((highest - lowest) / 2);
            return midPoint;
        }
    }
}