using EZCameraShake;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : Singleton<CameraController>
{
    [SerializeField] bool shakeCamera;

    [SerializeField] Mode mode;

    [SerializeField] Camera cam;

    public float zoom = 1.0f;
    public float zoomSpeed = 1.0f;

    float currentZoom = 1;

    private int direction;
    public float camHeight = 10;
    public float camDistance = 10;
    private bool locked;
    private bool rotate;

    public enum Mode 
    {
        Normal,
        Custom
    }

    Transform startCameraAnchor;

    bool zoomEnabled = true;

    [SerializeField] float crashZoom = 0.7f;
    [SerializeField] float crashZoomSpeed = 0.25f;
    [SerializeField] float snapSpeed = 0.25f;
    [SerializeField] float moveSpeed = 0.125f;
    [SerializeField] float chaseDistance = 2.0f;
    [SerializeField] bool moveOffset;

    WheelVehicle target;

    Vector3 offset;
    Quaternion chaseRotation;
    bool initChaseRotation;
    Transform anchor;

    public Vector3 customCameraOffset;
    public float customChaseSpeed = 0.1f;

    Transform customAnchor;

    public enum CameraState 
    {
        None,
        Start,
        Chase,
        Crash,
    }

    CameraState state;
    public CameraState State 
    {
        get => state;
        set 
        {
            if (value != state) 
            {
                if (value != CameraState.Chase)
                    initChaseRotation = false;

                switch (value) 
                {
                    case CameraState.Chase:

                        if (mode == Mode.Normal)
                        {
                            var camPos = target.transform.position;

                            camPos.z -= camDistance;
                            camPos.y = camHeight;

                            offset = camPos - target.transform.position;
                            chaseRotation = Quaternion.LookRotation(target.transform.position - camPos, Vector3.up);
                            shiftingRotationAngles = chaseRotation.eulerAngles;
                            initChaseRotation = true;
                        }
                        else 
                        {
                            if (customAnchor)
                                Destroy(customAnchor.gameObject);

                            customAnchor = new GameObject("customAnchor").transform;
                            customAnchor.SetParent(target.transform);
                            customAnchor.localPosition += customCameraOffset;
                        }

                        break;
                }
            }

            state = value;
        }
    }

    public bool IsSnapingToAnchor { get; private set; }

    private float Clamp0360(float eulerAngles)
    {
        float result = eulerAngles - Mathf.CeilToInt(eulerAngles / 360f) * 360f;
        if (result < 0)
        {
            result += 360f;
        }
        return result;
    }

    enum Direction 
    {
        Up,
        Down,
        Left,
        Right
    }

    private Direction GetTargetDirection() 
    {
        float rotation = Clamp0360(target.transform.eulerAngles.y);

        if (rotation >= 0 && rotation < -90.0f) return Direction.Down;
        else if (rotation >= 90.0f && rotation < -180.0f) return Direction.Left;
        else if (rotation >= 90.0f && rotation < -180.0f) return Direction.Up;
        else return Direction.Right;
    }

    void LateUpdate()
    {
        if (state == CameraState.None) return;

        if (mode == Mode.Normal)
        {
            if (state == CameraState.Start)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, startCameraAnchor.rotation, snapSpeed);
                transform.position = Vector3.Lerp(transform.position, startCameraAnchor.position, moveSpeed);
            }
            else if (state == CameraState.Chase)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, chaseRotation, target.Move ? snapSpeed : 0.05f);

                currentZoom = Mathf.Lerp(currentZoom, zoom, zoomSpeed);

                transform.position = Vector3.Lerp(
                    transform.position,
                    moveOffset ? target.chaseTransform.position : target.transform.position +  offset * currentZoom,
                    state == CameraState.Crash ? moveSpeed * 0.1f : moveSpeed);
            }
        }
        else if (mode == Mode.Custom)
        {
            if (state == CameraState.Chase)
            {
                if(customAnchor)
                    transform.position = Vector3.Lerp(transform.position, target.cameraAnchor.position, customChaseSpeed * Time.fixedDeltaTime);
            }
        }
    }

    public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        Vector3 dir = point - pivot; // get point direction relative to pivot
        dir = Quaternion.Euler(angles) * dir; // rotate it
        point = dir + pivot; // calculate rotated point
        return point; // return it
    }

    private void Update()
    {
        //if (state == CameraState.Chase)
        //    transform.LookAt(target.transform);
    }

    float shiftingInput;
    Vector3 shiftingRotationAngles;

    public void ApplyShifting(float input) 
    {
        if (shiftingInput != input) 
        {
            shiftingInput = input;

            StopCoroutine("ApplyShiftingCoroutine");
            chaseRotation.eulerAngles = shiftingRotationAngles;

            if (initChaseRotation)
            {
                StartCoroutine("ApplyShiftingCoroutine", input);
            }
        }
    }

    WaitForSeconds shiftingDelay = new WaitForSeconds(1.0f);

    IEnumerator ApplyShiftingCoroutine(float input) 
    {
        yield return shiftingDelay;

        chaseRotation.eulerAngles = new Vector3(
            chaseRotation.eulerAngles.x, 
            chaseRotation.eulerAngles.y + (20.0f * input), 
            chaseRotation.eulerAngles.z);
    }

    public void SetStartAnchor(Transform anchor)
    {
        this.startCameraAnchor = anchor;
    }

    public void SetTarget(WheelVehicle target)
    {
        this.target = target;
    }

    public void CameraShake(float duration, CameraShakeMode mode) 
    {
        if(shakeCamera)
        {
            float influence = 0;

            if(mode == CameraShakeMode.Week) influence = 0.7f;
            else if (mode == CameraShakeMode.Medium) influence = 1.0f;
            else if (mode == CameraShakeMode.Strong) influence = 1.5f;

            CameraShaker.Instance.DefaultRotInfluence = new Vector3(influence, influence, influence);
            CameraShaker.Instance.ShakeOnce(1.0f, 2.0f, duration / 2, duration / 2);
		}
    }

    public enum CameraShakeMode 
    {
        Week,
        Medium,
        Strong
    }
}
