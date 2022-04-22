using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CurieSDK
{
    public class CurieAPICall<T> where T : class
    {
        public Coroutine Coroutine { get; private set; }
        public bool Completed { get; private set; }

        private IEnumerator target;
        private Action<T> _onComplete;
        private object result;

        public CurieAPICall(MonoBehaviour owner, IEnumerator target, Action<T> onComplete)
        {
            this.Completed = false;
            this.target = target;
            this.Coroutine = owner.StartCoroutine(Run());
            _onComplete = onComplete;
        }

        public T GetResult()
        {
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