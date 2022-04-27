using System;
using System.Collections;
using UnityEngine;

namespace CurieSDK
{
    public class CurieAPICall<T> where T : class
    {
        /// <summary>
        /// Yieldable within an IEnumerator Coroutine. On yield, the result will be available and the API call will be complete.
        /// </summary>
        public Coroutine Coroutine { get; private set; }

        /// <summary>
        /// Returns the status of the API Call
        /// </summary>
        public bool Completed { get; private set; }

        private IEnumerator target;
        private Action<T> _onComplete;
        private object result;

        /// <summary>
        /// Curie API call. Can be yieled in a couroutine using 'Yield CurieAPICall.Coroutine' or it's status
        /// can be checked with CurieAPICall.Completed
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="target"></param>
        /// <param name="onComplete"></param>
        public CurieAPICall(MonoBehaviour owner, IEnumerator target, Action<T> onComplete)
        {
            this.Completed = false;
            this.target = target;
            this.Coroutine = owner.StartCoroutine(Run());
            _onComplete = onComplete;
        }

        /// <summary>
        /// Get the result of the API call once it's complete. Will always return null if the API call has not resolved.
        /// </summary>
        /// <returns></returns>
        public T GetResult()
        {
            if (!Completed) return null;

            return result as T;
        }

        private IEnumerator Run()
        {
            while (target.MoveNext())
            {
                result = target.Current;
                yield return result;
            }

            _onComplete?.Invoke(result as T);
            this.Completed = true;
        }
    }
}