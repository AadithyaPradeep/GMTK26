
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using UnityEngine.InputSystem;

public class GrabCluck : MonoBehaviour
{

    public HighlightCluck hc;
 
    public Animator aimAnimator;
    private Transform grabbedCluck;
    public GameObject hand;

 

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
                grabbedCluck = null;
                aimAnimator.SetBool("Grabbed",false);
                grabbedCluck.transform.position = new Vector2(transform.position.x + (0.5f * Convert.ToInt32(GetComponent<SpriteRenderer>().flipX)), transform.position.y);

            }
            grabbedCluck = hc.GetSelectedClucks().transform;
            grabbedCluck.SetParent(transform);
            grabbedCluck.localPosition = new Vector3(0,0.3f,0);
            grabbedCluck.GetComponent<BoxCollider2D>().enabled = false;
            grabbedCluck.GetComponent<ChickenWander>().enabled = false;
            aimAnimator = grabbedCluck.GetComponent<Animator>();
            aimAnimator.SetBool("Grabbed",true);
        }
        
       
    }

}