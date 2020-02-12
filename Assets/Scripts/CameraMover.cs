using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

/// <summary>
/// Camera operation
/// </summary>
public class CameraMover : MonoBehaviour
{
    // WASD：movement in four quarters
    // QE：up and down
    // drag right：rotate camera
    // drag left：movement in quarters
    // space：swith on/off of camera opperation
    // P：initialize rotation

    /// <summary>
    /// amount of movement
    /// </summary>
    [SerializeField, Range(0.1f, 10.0f)]
    private float _positionStep = 2.0f;

    /// <summary>
    /// mouse sensitivity
    /// </summary>
    [SerializeField, Range(30.0f, 150.0f)]
    private float _mouseSensitive = 90.0f;

    /// <summary>
    /// switch on/off of opperation of camera
    /// </summary>
    private bool _cameraMoveActive = true;

    /// <summary>
    /// camera transformation
    /// </summary>
    private Transform _camTransform;

    /// <summary>
    /// start position of camera
    /// </summary>
    private Vector3 _startMousePos;
    
    /// <summary>
    /// Current camera rotate angle
    /// </summary>
    private Vector3 _presentCamRotation;

    /// <summary>
    /// Current camera positoin
    /// </summary>
    private Vector3 _presentCamPos;
    
    /// <summary>
    /// initial condtion of rotation
    /// </summary>
    private Quaternion _initialCamRotation;
    
    /// <summary>
    /// activation of UI message
    /// </summary>
    private bool _uiMessageActiv;

    void Start()
    {
        _camTransform = this.gameObject.transform;

        // reserve initialize rotation
        _initialCamRotation = this.gameObject.transform.rotation;
    }

    void Update()
    {

        CamControlIsActive();

        if (_cameraMoveActive)
        {
            ResetCameraRotation();
            CameraRotationMouseControl();
            CameraSlideMouseControl();
            CameraPositionKeyControl();
        }
    }

    /// <summary>
    /// activate/deactivate camera opperation
    /// </summary>
    public void CamControlIsActive()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _cameraMoveActive = !_cameraMoveActive;

            if (_uiMessageActiv == false)
            {
                StartCoroutine(DisplayUiMessage());
            }
            Debug.Log("CamControl : " + _cameraMoveActive);
        }
    }

    /// <summary>
    /// reset camera rotation
    /// </summary>
    private void ResetCameraRotation()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            this.gameObject.transform.rotation = _initialCamRotation;
            Debug.Log("Cam Rotate : " + _initialCamRotation.ToString());
        }
    }

    /// <summary>
    /// Camera rotation by mouse
    /// </summary>
    private void CameraRotationMouseControl()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _startMousePos = Input.mousePosition;
            _presentCamRotation.x = _camTransform.transform.eulerAngles.x;
            _presentCamRotation.y = _camTransform.transform.eulerAngles.y;
        }

        if (Input.GetMouseButton(0))
        {
            // Normalization = (start position - current position) / resolution
            float x = (_startMousePos.x - Input.mousePosition.x) / Screen.width;
            float y = (_startMousePos.y - Input.mousePosition.y) / Screen.height;

            // current rotate angle ＋ movement amount * mouse sensitivity
            float eulerX = _presentCamRotation.x + y * _mouseSensitive;
            float eulerY = _presentCamRotation.y + x * _mouseSensitive;

            _camTransform.rotation = Quaternion.Euler(eulerX, eulerY, 0);
        }
    }

    /// <summary>
    /// Move camera by mouse
    /// </summary>
    private void CameraSlideMouseControl()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _startMousePos = Input.mousePosition;
            _presentCamPos = _camTransform.position;
        }

        if (Input.GetMouseButton(1))
        {
            // Normalization: (start position - current position) / resolution
            float x = (_startMousePos.x - Input.mousePosition.x) / Screen.width;
            float y = (_startMousePos.y - Input.mousePosition.y) / Screen.height;

            x = x * _positionStep;
            y = y * _positionStep;

            Vector3 velocity = _camTransform.rotation * new Vector3(x, y, 0);
            velocity = velocity + _presentCamPos;
            _camTransform.position = velocity;
        }
    }

    /// <summary>
    /// Local movement of camera by key
    /// </summary>
    private void CameraPositionKeyControl()
    {
        Vector3 campos = _camTransform.position;

        if (Input.GetKey(KeyCode.D)) { campos += _camTransform.right * Time.deltaTime * _positionStep; }
        if (Input.GetKey(KeyCode.A)) { campos -= _camTransform.right * Time.deltaTime * _positionStep; }
        if (Input.GetKey(KeyCode.E)) { campos += _camTransform.up * Time.deltaTime * _positionStep; }
        if (Input.GetKey(KeyCode.Q)) { campos -= _camTransform.up * Time.deltaTime * _positionStep; }
        if (Input.GetKey(KeyCode.W)) { campos += _camTransform.forward * Time.deltaTime * _positionStep; }
        if (Input.GetKey(KeyCode.S)) { campos -= _camTransform.forward * Time.deltaTime * _positionStep; }

        _camTransform.position = campos;
    }

    /// <summary>
    /// Show UI message
    /// </summary>
    /// <returns></returns>
    private IEnumerator DisplayUiMessage()
    {
        _uiMessageActiv = true;
        float time = 0;
        while (time < 2)
        {
            time = time + Time.deltaTime;
            yield return null;
        }
        _uiMessageActiv = false;
    }

    void OnGUI()
    {
        if (_uiMessageActiv == false) { return; }
        GUI.color = Color.black;
        if (_cameraMoveActive == true)
        {
            GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height - 30, 100, 20), "Active Camera Operation");
        }

        if (_cameraMoveActive == false)
        {
            GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height - 30, 100, 20), "Deactive Camera Operation");
        }
    }

}