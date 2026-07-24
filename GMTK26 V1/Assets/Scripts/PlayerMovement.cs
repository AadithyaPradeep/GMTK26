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
    [SerializeField] private float stepInterval = 0.28f;

    private float stepTimer;

    private void Update()
    {
        Vector2 live = GetLiveDirection();

        if (live.x < 0)
        {
            spriteRenderer.flipX = true;

            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            {
                sr.flipX = spriteRenderer.flipX;
            }

            animator.SetBool("Running", true);
        }
        if (live.x > 0)
        {
            spriteRenderer.flipX = false;

            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            {
                sr.flipX = spriteRenderer.flipX;
            }

            animator.SetBool("Running", true);
        }
        if (live.x == 0)
        {
            animator.SetBool("Running", false);
        }
        if (live.y != 0)
        {
            animator.SetBool("Running", true);
        }

        UpdateFootsteps(live);
    }

    private void UpdateFootsteps(Vector2 live)
    {
        bool moving = live.sqrMagnitude > 0.01f;
        if (!moving)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer -= Time.deltaTime;
        if (stepTimer > 0f)
            return;

        stepTimer = stepInterval;
        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayStep();
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