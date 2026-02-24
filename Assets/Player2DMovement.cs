using UnityEngine;

public class Player2DMovement : MonoBehaviour
{
    public static Player2DMovement Instance { get; private set; }
    [SerializeField] private float moveSpeed = 5f;
    private float defaultMoveSpeed;
    private InputSystem_Actions inputActions;
    private Vector2 movementInput;
    private Vector2 lookInput;

    private Rigidbody2D rb;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        inputActions = new InputSystem_Actions();
        inputActions.Player.Move.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => movementInput = Vector2.zero;
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;
        inputActions.Player.Sprint.performed += ctx => moveSpeed = moveSpeed * 1.5f; // Example sprint speed
        inputActions.Player.Sprint.canceled += ctx => moveSpeed = defaultMoveSpeed;
        inputActions.Enable();
    }

    void Start()
    {
        defaultMoveSpeed = moveSpeed;
    }
 
    void OnDestroy()
    {
        inputActions.Disable();
    }

    void FixedUpdate()
    {
        // Apply movement using Rigidbody2D for proper physics
        rb.linearVelocity = movementInput * moveSpeed;
    }

    void Update()
    {
        // Handle rotation based on look input
        if (lookInput != Vector2.zero)
        {
            float angle = Mathf.Atan2(lookInput.y, lookInput.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
}