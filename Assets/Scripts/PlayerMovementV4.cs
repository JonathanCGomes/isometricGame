// PlayerMovementV4 atualizado
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerMovementV4 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private int maxDashCharges = 1;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference dashAction;

    private Vector2 input;
    private Vector3 velocity;
    private bool isGrounded;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    // Dash control
    private List<DashState> dashStates;
    private Vector3 dashDirection;
    private bool isDashing = false;

    private enum DashState { Ready, Cooldown }

    private void Start()
    {
        dashStates = new List<DashState>();
        for (int i = 0; i < maxDashCharges; i++)
        {
            dashStates.Add(DashState.Ready);
        }
    }

    private void OnEnable()
    {
        jumpAction.action.performed += OnJump;
    }

    private void OnDisable()
    {
        jumpAction.action.performed -= OnJump;
    }

    private void Update()
    {
        HandleInput();
        CheckGrounded();
        HandleDash();

        HandleJump(); // jump sempre funciona dashando ou nao

        if (!isDashing) // movimentacao bloqueada apenas no dash
        {
            Move();
        }

        ApplyGravity();
    }

    private void HandleInput()
    {
        input = moveAction.action.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        jumpBufferCounter = jumpBufferTime;
    }

    private void CheckGrounded()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void HandleJump()
    {
        jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            velocity.y = jumpForce;
            jumpBufferCounter = 0f;
        }
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void Move()
    {
        Vector3 move = new Vector3(input.x, 0f, input.y);

        if (move.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * moveSpeed * Time.deltaTime);
        }
    }

    private void HandleDash()
    {
        if (dashAction.action.triggered && !isDashing)
        {
            Vector3 moveInput = new Vector3(input.x, 0f, input.y);

            if (moveInput.magnitude < 0.1f)
            {
                // nao faz dash sem direcao
                return;
            }

            for (int i = 0; i < dashStates.Count; i++)
            {
                if (dashStates[i] == DashState.Ready)
                {
                    StartCoroutine(PerformDash(moveInput, i));
                    dashStates[i] = DashState.Cooldown;
                    break;
                }
            }
        }
    }

    private IEnumerator PerformDash(Vector3 moveInput, int dashIndex)
    {
        isDashing = true;

        float targetAngle = Mathf.Atan2(moveInput.x, moveInput.z) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
        dashDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

        float dashTime = dashDuration;

        while (dashTime > 0f)
        {
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
            dashTime -= Time.deltaTime;
            yield return null;
        }

        isDashing = false;
        StartCoroutine(RechargeDash(dashIndex));
    }

    private IEnumerator RechargeDash(int dashIndex)
    {
        yield return new WaitForSeconds(dashCooldown);
        dashStates[dashIndex] = DashState.Ready;
    }
}
