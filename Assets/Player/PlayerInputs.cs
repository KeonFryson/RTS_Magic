using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputs : MonoBehaviour
{
    [SerializeField] public int Damage = 1;
    [SerializeField] public float Range = 1f;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Get mouse position in world space
            Vector3 mouseScreen = Mouse.current.position.ReadValue();
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -Camera.main.transform.position.z));

            // Find all resource nodes in range
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, Range);
            ResourceNode closestNode = null;
            float closestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var resourceNode = hit.GetComponent<ResourceNode>();
                if (resourceNode != null)
                {
                    float dist = Vector2.Distance(mouseWorld, resourceNode.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestNode = resourceNode;
                    }
                }
            }

            // Damage the closest node to the mouse, if any
            if (closestNode != null)
            {
                closestNode.TakeDamage(Damage);
            }
        }
    }

    // Optional: visualize mining range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Range);
    }
}