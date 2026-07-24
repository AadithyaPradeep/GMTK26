
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

using UnityEngine.InputSystem;

public class GrabCluck : MonoBehaviour
{

    public HighlightCluck hc;
 
    public Animator aimAnimator;
    private Transform grabbedCluck;
    public GameObject hand;
    public CinemachineImpulseSource source;
    public float strength;
 

    private void Awake()
    {

        
    }

    private void Update()
    {
        if(Keyboard.current.eKey.wasPressedThisFrame)
        {
            if(grabbedCluck != null)
            {
                grabbedCluck.GetComponent<ChickenWander>().enabled = true;
                grabbedCluck.GetComponent<BoxCollider2D>().enabled = true;
                grabbedCluck.SetParent(null);
                float offset = (GetComponent<SpriteRenderer>().flipX) ? -2 : 2;
                grabbedCluck.transform.position = new Vector2(transform.position.x + offset, transform.position.y);
                grabbedCluck = null;
                aimAnimator.SetBool("Grabbed",false);
                if (GameAudio.Instance != null)
                    GameAudio.Instance.PlayDrop();
            }
            else
            {
                ChickenWander selected = hc.GetSelectedClucks();
                if (selected == null) return;
                grabbedCluck = selected.transform;
                grabbedCluck.SetParent(transform);
                Vector3 dif = grabbedCluck.transform.position - transform.position;
                source.GenerateImpulseWithVelocity(strength * dif.normalized);
                grabbedCluck.localPosition = new Vector3(0, 0.3f, 0);
                grabbedCluck.GetComponent<BoxCollider2D>().enabled = false;
                grabbedCluck.GetComponent<ChickenWander>().enabled = false;
                aimAnimator = grabbedCluck.GetComponent<Animator>();
                aimAnimator.SetBool("Grabbed", true);
                if (GameAudio.Instance != null)
                    GameAudio.Instance.PlayGrab();
            }
            
        }
        
       
    }

}