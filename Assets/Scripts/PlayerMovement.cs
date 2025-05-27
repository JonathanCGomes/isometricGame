using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraTransform;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference dashAction;
    [SerializeField] private InputActionReference lockOnAction;
    [SerializeField] private InputActionReference switchTargetLeftAction;
    [SerializeField] private InputActionReference switchTargetRightAction;
    [SerializeField] private InputActionReference lookAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private int maxDashCharges = 2;

    [Header("Lock On")]
    [SerializeField] private float lockOnRadius = 15f;
    [SerializeField] private LayerMask enemyLayer;

    // Movement
    private Vector2 input;
    private Vector3 velocity;
    private bool isGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    // Dash
    private enum DashState { Ready, Cooldown }
    private List<DashState> dashStates;
    private bool isDashing;
    private Vector3 dashDirection;

    // Lock-On
    private bool isLockOnActive = false;
    private Transform currentTarget;
    private List<Transform> targetsInRange = new List<Transform>();
    private int currentTargetIndex = 0;

    private void Start()
    {
        dashStates = new List<DashState>();
        for (int i = 0; i < maxDashCharges; i++)
        {
            dashStates.Add(DashState.Ready);
        }
    }

    private void Update()
    {
        HandleInput();
        CheckGrounded();

        if (!isDashing)
        {
            HandleMovement();
        }

        HandleJump();
        HandleDash();
        HandleLockOn();
        HandleSwitchTarget();
    }

    private void HandleInput()
    {
        input = moveAction.action.ReadValue<Vector2>();
    }

    private void CheckGrounded()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        Vector3 move = new Vector3(input.x, 0f, input.y);

        if (move.magnitude >= 0.1f)
        {
            Vector3 moveDirection = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f) * move;
            moveDirection.Normalize();

            if (isLockOnActive && currentTarget != null)
            {
                // Apenas rotaciona mirando no alvo
                RotateTowards(currentTarget.position);
            }
            else
            {
                // Mira livre com Look Stick
                Vector2 lookInput = lookAction.action.ReadValue<Vector2>();
                if (lookInput.magnitude > 0.1f)
                {
                    Vector3 lookDirection = new Vector3(lookInput.x, 0f, lookInput.y);
                    lookDirection = Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f) * lookDirection;
                    RotateTowards(transform.position + lookDirection);
                }
                else
                {
                    RotateTowards(transform.position + moveDirection);
                }
            }

            controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (jumpAction.action.triggered)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferCounter = 0f;
        }
    }

    private void HandleDash()
    {
        if (dashAction.action.triggered)
        {
            Vector3 moveInput = new Vector3(input.x, 0f, input.y);

            if (moveInput.magnitude < 0.1f && !isLockOnActive)
            {
                return; // Nao permite dash sem direcao se nao estiver em lock-on
            }

            for (int i = 0; i < dashStates.Count; i++)
            {
                if (dashStates[i] == DashState.Ready)
                {
                    dashDirection = (Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f) * moveInput.normalized);
                    StartCoroutine(PerformDash(i));
                    dashStates[i] = DashState.Cooldown;
                    break;
                }
            }
        }
    }

    private IEnumerator PerformDash(int dashIndex)
    {
        isDashing = true;
        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {
            controller.Move(dashDirection * dashSpeed * Time.deltaTime);
            yield return null;
        }

        isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
        dashStates[dashIndex] = DashState.Ready;
    }

    private void HandleLockOn()
    {
        if (lockOnAction.action.triggered)
        {
            if (isLockOnActive)
            {
                isLockOnActive = false;
                currentTarget = null;
            }
            else
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, lockOnRadius, enemyLayer);
                if (hits.Length > 0)
                {
                    targetsInRange.Clear();
                    foreach (Collider hit in hits)
                    {
                        targetsInRange.Add(hit.transform);
                    }
                    targetsInRange.Sort((a, b) => Vector3.Distance(transform.position, a.position).CompareTo(Vector3.Distance(transform.position, b.position)));
                    currentTargetIndex = 0;
                    currentTarget = targetsInRange[currentTargetIndex];
                    isLockOnActive = true;
                }
            }
        }
    }

    private void HandleSwitchTarget()
    {
        if (!isLockOnActive || targetsInRange.Count == 0)
            return;

        if (switchTargetRightAction.action.triggered)
        {
            currentTargetIndex = (currentTargetIndex + 1) % targetsInRange.Count;
            currentTarget = targetsInRange[currentTargetIndex];
        }

        if (switchTargetLeftAction.action.triggered)
        {
            currentTargetIndex--;
            if (currentTargetIndex < 0) currentTargetIndex = targetsInRange.Count - 1;
            currentTarget = targetsInRange[currentTargetIndex];
        }
    }

    private void RotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;
        if (direction == Vector3.zero) return;

        Quaternion toRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lockOnRadius);
    }
}
