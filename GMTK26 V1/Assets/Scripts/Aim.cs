
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

using UnityEngine.InputSystem;

public class Aim : MonoBehaviour
{

   
    public event EventHandler<OnShootEventArgs> OnShoot;
    private Vector3 lookAtPosition;
    
    public class OnShootEventArgs : EventArgs
    {
        public Vector3 gunEndPointPosition;
        public Vector3 shootPosition;
        public Vector3 shellPosition;

    }

    public CinemachineImpulseSource source;
    public Transform aimTransform;
   
    public Animator aimAnimator;


    

    private void Awake()
    {

    }

    private void Update()
    {
       
        HandleAiming();
        

    }

    private void HandleAiming()
    {
        Vector3 mousePosition = GetMouseWorldPosition();

        Vector3 aimDirection = (mousePosition - aimTransform.position).normalized;
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        aimTransform.eulerAngles = new Vector3(0, 0, angle);

        Vector3 aimLocalScale = Vector3.one;
        if (angle > 90 || angle < -90)
        {
            aimLocalScale.y = -1f;
        }
        else
        {
            aimLocalScale.y = +1f;
        }
        aimTransform.localScale = aimLocalScale;

        SetLookAtPosition(mousePosition);
    }

    
    public static Vector3 GetMouseWorldPosition()
    {
        Vector3 vec = GetMouseWorldPositionWithZ(Mouse.current.position.value, Camera.main);
        vec.z = 0f;
        return vec;
    }
    public static Vector3 GetMouseWorldPositionWithZ()
    {
        return GetMouseWorldPositionWithZ(Mouse.current.position.value, Camera.main);
    }
    public static Vector3 GetMouseWorldPositionWithZ(Camera worldCamera)
    {
        return GetMouseWorldPositionWithZ(Mouse.current.position.value, worldCamera);
    }
    public static Vector3 GetMouseWorldPositionWithZ(Vector3 screenPosition, Camera worldCamera)
    {
        screenPosition.z = Mathf.Abs(worldCamera.transform.position.z);
        Vector3 worldPosition = worldCamera.ScreenToWorldPoint(screenPosition);
        return worldPosition;
    }
    private void HandleLookAtMouse()
    {
        lookAtPosition = GetMouseWorldPosition();
    }
    public void SetLookAtPosition(Vector3 lookAtPosition)
    {
        this.lookAtPosition = lookAtPosition;
    }
    
}