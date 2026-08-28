using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementCC : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;

    private CharacterController controller;

    private Vector2 moveInput;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Move();
        ApplyGravity();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void Move()
    {
        Vector3 movement =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        controller.Move(movement * moveSpeed * Time.deltaTime);
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
}