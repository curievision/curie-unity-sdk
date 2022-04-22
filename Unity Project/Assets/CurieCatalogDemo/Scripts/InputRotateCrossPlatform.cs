using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CurieCatalogExample
{
    public class InputRotateCrossPlatform: MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float rotateSpeed = 20;
        public void Update()
        {
            if(Input.GetMouseButton(0))
            {
                var x = Input.GetAxis("Mouse X");
                var y = Input.GetAxis("Mouse Y");

                target.Rotate(Vector3.up, x * rotateSpeed * Time.deltaTime);
                target.Rotate(Vector3.right, y * rotateSpeed * Time.deltaTime);

            }
        }
    }
}
