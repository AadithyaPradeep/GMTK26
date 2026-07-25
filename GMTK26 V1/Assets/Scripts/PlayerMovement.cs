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
    private float speedMultiplier = 1f;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float stepInterval = 0.28f;

    private float stepTimer;

    // Wave-6: locked to left edge, vertical movement only, always face right.
    private bool verticalLaneMode;
    private float laneX;
    private float laneYMin = -4.8f;
    private float laneYMax = 4.1f;

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    public void SetBossLaneMode(bool enabled, float x, float yMin, float yMax)
    {
        verticalLaneMode = enabled;
        laneX = x;
        laneYMin = yMin;
        laneYMax = yMax;

        if (!enabled)
            return;

        Vector3 pos = transform.position;
        pos.x = laneX;
        pos.y = Mathf.Clamp(pos.y, laneYMin, laneYMax);
        transform.position = pos;

        FaceRight();
    }

    private void FaceRight()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX = false;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            sr.flipX = false;
    }

    private void Update()
    {
        Vector2 live = GetLiveDirection();

        if (verticalLaneMode)
        {
            FaceRight();
            bool moving = Mathf.Abs(live.y) > 0.01f;
            if (animator != null)
                animator.SetBool("Running", moving);
            UpdateFootsteps(moving ? new Vector2(0f, live.y) : Vector2.zero);
            return;
        }

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
        if (!movementKeyDown)
        {
            if (verticalLaneMode)
                LockLaneX();
            return;
        }

        if (verticalLaneMode)
        {
            float dy = movementVector.y * speed * speedMultiplier * Time.deltaTime;
            Vector3 pos = transform.position;
            pos.x = laneX;
            pos.y = Mathf.Clamp(pos.y + dy, laneYMin, laneYMax);
            transform.position = pos;
            return;
        }

        transform.position += speed * speedMultiplier * Time.deltaTime * new Vector3(movementVector.x, movementVector.y, 0);
    }

    private void LockLaneX()
    {
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.x - laneX) > 0.001f)
        {
            pos.x = laneX;
            transform.position = pos;
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
        if (verticalLaneMode)
            return new Vector2(0f, movementVector.y);
        return movementVector;
    }

}
