using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


[RequireComponent(typeof(CharacterController))]
public class PlayerMovementCC : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [Header("Jump")]
    [SerializeField] private float jumpHeight = 3.5f;
    [SerializeField] private float gravity = -9.81f;

    private CharacterController controller;
    private Vector2 moveInput;
    private float verticalVelocity;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!IsOwner || EscapeMenu.IsOpen)
            return;

        Move();
        UpdateAnimator();
        ApplyGravity();
    }

    // OnNetworkSpawn eliminado de acá — esa responsabilidad ya la tiene PlayerCameraSetup.

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (context.performed && controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            // if (animator != null)
            //   animator.SetTrigger("jump");
        }


    }

    private NetworkVariable<float> speedMultiplier = new NetworkVariable<float>(
    1f,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);
    private void Move()
    {
        Vector3 movement =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;
        controller.Move(movement * moveSpeed * speedMultiplier.Value * Time.deltaTime);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat("speed", moveInput.magnitude);
    }
    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }
        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(
            Vector3.up * verticalVelocity * Time.deltaTime
        );
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (!IsServer) return;
        StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    private IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        speedMultiplier.Value = multiplier;
        yield return new WaitForSeconds(duration);
        speedMultiplier.Value = 1f;
    }
}