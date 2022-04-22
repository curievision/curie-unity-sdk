using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurieCatalogExample
{
    using UnityEngine;
    using System.Collections;
    using UnityEngine.EventSystems;

    public class MouseOrbitImproved : MonoBehaviour
    {


        public Vector3 targetOffset;
        private Vector3 FirstPosition;
        private Vector3 SecondPosition;
        private Vector3 delta;
        private Vector3 lastOffset;

        public CurieModelViewerUI _modelViewerDemo;
        private Camera _camera;
        public GameObject _scene;

        private bool _clicked;
        public Transform target;
        public GameObject ObjectToScale;
        public float distance = 5.0f;
        
        public float xSpeed = 120.0f;
        public float ySpeed = 120.0f;

        public float yMinLimit = -20f;
        public float yMaxLimit = 80f;

        public float distanceMin = .5f;
        public float distanceMax = 15f;
        public float zoomSpeed = 1f;
        public float moveSpeed = 1f;

        private Rigidbody rigidbody;


        public RotateObject Rotator;
        private float timeSinceLastClick = 0;
        private float timeSinceLastPinchToZoom = 0;

        float x = 0.0f;
        float y = 0.0f;

        // Use this for initialization
        void Start()
        {
            
            _clicked = false;
            _camera = GetComponent<Camera>();
            Vector3 angles = transform.eulerAngles;
            x = angles.y;
            y = angles.x;

            rigidbody = GetComponent<Rigidbody>();

            // Make the rigid body not change rotation
            if (rigidbody != null)
            {
                rigidbody.freezeRotation = true;
            }

            DoOrbit();
        }


        void LateUpdate()
        {
            timeSinceLastClick += Time.deltaTime;
            if(Input.GetMouseButton(0) && !MouseOrTouchOverUI())
            {
                timeSinceLastClick = 0;
            }

            Rotator.Rotate = timeSinceLastClick > 8.0f;


            if (target)
            {
                //_camera.fieldOfView += Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
                //_camera.fieldOfView = Mathf.Clamp(_camera.fieldOfView, 3, 100);
            }

            if (Input.GetKey(KeyCode.Q))
            {
                _scene.transform.position += Vector3.up * Time.deltaTime * moveSpeed;
            }
            if (Input.GetKey(KeyCode.E))
            {
                _scene.transform.position -= Vector3.up * Time.deltaTime * moveSpeed;
            }


            if (target)
            {
                DoOrbit();
            }
        }

        private bool MouseOrTouchOverUI()
        {
            var touched = false;
            foreach (Touch touch in Input.touches)
            {
                int id = touch.fingerId;
                if (EventSystem.current.IsPointerOverGameObject(id))
                {
                    touched = true;
                    break;
                }
            }
            var overUI = EventSystem.current.IsPointerOverGameObject() || touched;
            //Debug.Log("Mouse over UI:" + overUI);
            return overUI;
        }

        public float ScaleUpSpeed = 1.05f;
        public float ScaleDownSpeed = 0.95f;

        void DoOrbit()
        {
            timeSinceLastPinchToZoom += Time.deltaTime;
            if(Input.touchCount >= 2)
            {
                timeSinceLastPinchToZoom = 0;
            }

            if (Input.GetMouseButton(0) && !MouseOrTouchOverUI() && timeSinceLastPinchToZoom > 0.1f)
            {
                x += Input.GetAxis("Mouse X") * xSpeed * distance * 0.02f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
            }
            

            y = ClampAngle(y, yMinLimit, yMaxLimit);

            Quaternion rotation = Quaternion.Euler(y, x, 0);

            //distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel") * 5, distanceMin, distanceMax);

            var scrollDir = Input.GetAxis("Mouse ScrollWheel");
            if(scrollDir > 0.01 || scrollDir < -0.01)
            {
                if(ObjectToScale != null)
                {
                    // Debug.Log("scrollDir:" + scrollDir);
                    var s = ObjectToScale.transform.localScale;

                    s.x *= scrollDir > 0 ? ScaleUpSpeed : ScaleDownSpeed;
                    s.y *= scrollDir > 0 ? ScaleUpSpeed : ScaleDownSpeed;
                    s.z *= scrollDir > 0 ? ScaleUpSpeed : ScaleDownSpeed;

                    ObjectToScale.transform.localScale = s;
                }
            }


            RaycastHit hit;
            if (Physics.Linecast(target.position, transform.position, out hit))
            {
                distance -= hit.distance;
            }
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
            Vector3 position = rotation * negDistance + target.position - Vector3.up;

            transform.rotation = rotation;

            if (Input.GetMouseButtonDown(1))
            {
                FirstPosition = Input.mousePosition;
                lastOffset = targetOffset;
            }

            if (Input.GetMouseButton(1))
            {
                SecondPosition = Input.mousePosition;
                delta = SecondPosition - FirstPosition;
                targetOffset = lastOffset + transform.right * delta.x * 0.003f + transform.up * delta.y * 0.003f;

            }
            position = position - targetOffset;

            transform.position = position;
        }

        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360F)
                angle += 360F;
            if (angle > 360F)
                angle -= 360F;
            return Mathf.Clamp(angle, min, max);
        }
    }
}
