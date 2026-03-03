using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerInputs : MonoBehaviour
{
    public static PlayerInputs Instance { get; private set; }

    [SerializeField] public int Damage = 1;
    [SerializeField] public float Range = 1f;
    [SerializeField, Range(1f, 180f)] public float ConeAngle = 45f; // Vision cone angle in degrees

    private InputSystem_Actions inputActions;

    // Events for menu toggling
    public event Action OnPauseMenu;
    public event Action OnCraftingMenu;
    public event Action OnStorageMenu;
    public event Action OnChatMenu;

    // Default click events
    public event Action<Vector3> OnClick;
    public event Action<Vector3> OnRightClick;

    private void Awake()
    {
        Instance = this; // <-- Add this line
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        // Mining logic (mouse left click)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector3 mouseScreen = Mouse.current.position.ReadValue();
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -Camera.main.transform.position.z));
            OnClick?.Invoke(mouseWorld);

            Vector2 mouseWorld2D = new Vector2(mouseWorld.x, mouseWorld.y);
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld2D, Vector2.zero);
            if (hit.collider != null)
            {
                var resourceNode = hit.collider.GetComponent<ResourceNode>();
                if (resourceNode != null)
                {
                    Vector2 playerPos = transform.position;
                    float dist = Vector2.Distance(playerPos, resourceNode.transform.position);
                    if (dist <= Range)
                    {
                        resourceNode.TakeDamage(Damage);
                    }
                }
            }
        }

        // Right click event
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Vector3 mouseScreen = Mouse.current.position.ReadValue();
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -Camera.main.transform.position.z));
            OnRightClick?.Invoke(mouseWorld);
        }

        // Menu toggling
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            OnPauseMenu?.Invoke();
        else if (Keyboard.current.cKey.wasPressedThisFrame)
            OnCraftingMenu?.Invoke();
        else if (Keyboard.current.leftBracketKey.wasPressedThisFrame)
            OnStorageMenu?.Invoke();
        else if (Keyboard.current.tabKey.wasPressedThisFrame)
            OnChatMenu?.Invoke();
    }

    // Visualize mining cone in editor
    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.yellow;
    //    Vector3 origin = transform.position;

    //    // Draw cone
    //    Vector3 mouseScreen = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    //    Vector3 mouseWorld = Camera.main != null
    //        ? Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -Camera.main.transform.position.z))
    //        : origin + Vector3.right;

    //    Vector2 dir = ((Vector2)mouseWorld - (Vector2)origin).normalized;
    //    float halfAngle = ConeAngle * 0.5f;

    //    Quaternion leftRot = Quaternion.Euler(0, 0, -halfAngle);
    //    Quaternion rightRot = Quaternion.Euler(0, 0, halfAngle);

    //    Vector2 leftDir = leftRot * dir;
    //    Vector2 rightDir = rightRot * dir;

    //    Gizmos.DrawLine(origin, origin + (Vector3)leftDir * Range);
    //    Gizmos.DrawLine(origin, origin + (Vector3)rightDir * Range);

    //    // Draw arc (approximate with lines)
    //    int segments = 20;
    //    for (int i = 0; i < segments; i++)
    //    {
    //        float t0 = -halfAngle + (ConeAngle * i) / segments;
    //        float t1 = -halfAngle + (ConeAngle * (i + 1)) / segments;
    //        Vector2 p0 = Quaternion.Euler(0, 0, t0) * dir * Range;
    //        Vector2 p1 = Quaternion.Euler(0, 0, t1) * dir * Range;
    //        Gizmos.DrawLine(origin + (Vector3)p0, origin + (Vector3)p1);
    //    }
    //}
}