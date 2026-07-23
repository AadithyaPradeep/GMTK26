using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{

    [SerializeField] private InputActionReference movement;
    public float speed = 1;
    private Vector2 movementVector;
    private bool movementKeyDown;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;



    private void Update()
    {


        if (GetLiveDirection().x < 0)
        {
            spriteRenderer.flipX = true;
            

            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            {
                sr.flipX = spriteRenderer.flipX;
            }
            
            animator.SetBool("Running", true);
        }
        if (GetLiveDirection().x > 0)
        {
            spriteRenderer.flipX = false;


            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            {
                sr.flipX = spriteRenderer.flipX;
            }

            animator.SetBool("Running", true);

        }
        if (GetLiveDirection().x == 0)
        {

            animator.SetBool("Running", false);
        }
        if (GetLiveDirection().y != 0)
        {
            animator.SetBool("Running", true);
        }

    }
    void OnEnable()
    {

        movement.action.Enable();
        movement.action.performed += OnMovementKeyDown;
        movement.action.canceled += OnMovementKeyUp;
    }



    void OnDisable()
    {
        movement.action.performed -= OnMovementKeyDown;
        movement.action.canceled -= OnMovementKeyUp;
        movement.action.Disable();
    }

    private void FixedUpdate()
    {
        if (movementKeyDown)
        {
            transform.position += speed * Time.deltaTime * new Vector3(movementVector.x, movementVector.y, 0);

        }
    }



    private void OnMovementKeyDown(InputAction.CallbackContext context)
    {
        movementKeyDown = true;


        var result = context.ReadValue<Vector2>();
        movementVector = result;



    }
    private void OnMovementKeyUp(InputAction.CallbackContext context)
    {
        movementKeyDown = false;
    }

    public Vector2 GetLastDirection()
    {
        return movementVector;
    }

    public Vector2 GetLiveDirection()
    {
        if (movementKeyDown == false) return Vector2.zero;
        return movementVector;
    }

}