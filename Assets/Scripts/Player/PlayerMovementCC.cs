using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementCC : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;

    private CharacterController controller;
    private PlayerHealth playerHealth;
    private Vector2 moveInput;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        // Si estoy muerto, no puedo moverme ni interactuar con el escenario.
        if (playerHealth != null && playerHealth.IsDead())
            return;
        Move();
        ApplyGravity();
    }

  
    public void OnMove(InputAction.CallbackContext context)
    {
        if (playerHealth != null && playerHealth.IsDead())
        {
            moveInput = Vector2.zero;
            return;
        }
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (playerHealth != null && playerHealth.IsDead())
            return;
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

    public void OnDisconnect(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Desconectar();
        }
    }
private void Desconectar()
{
    if (NetworkManager.Singleton == null)
        return;

    NetworkManager.Singleton.StartCoroutine(
        DesconectarYVolverAlMenu()
    );
}

private System.Collections.IEnumerator DesconectarYVolverAlMenu()
{
    NetworkManager.Singleton.Shutdown();

    yield return new WaitForSeconds(0.2f);

    SceneManager.LoadScene("menuPrincipal");
}
}