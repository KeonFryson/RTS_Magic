using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    [SerializeField] private InventoryItem resourceItem;
    [SerializeField] public int minquantity = 1;
    [SerializeField] public int maxquantity = 5;
    [SerializeField] public int Heath = 10;

    public void TakeDamage(int damage)
    {
        Heath -= damage;
        if (Heath <= 0)
        {
            Harvest();
        }
    }

    public void Harvest()
    {
        if (resourceItem != null)
        {
            int quantityToAdd = UnityEngine.Random.Range(minquantity, maxquantity);
            for (int i = 0; i < quantityToAdd; i++)
            {
                Inventory.Instance.AddItem(resourceItem);
            }
            Debug.Log($"Harvested {resourceItem.name} and added to inventory.");
        }
        else
        {
            Debug.LogWarning("No resource item assigned to this node.");
        }

        // Mark this node as destroyed
        Vector3Int cellPos = WorldGen.Instance.tilemap.WorldToCell(transform.position);
        DestroyedResourceNodesManager.MarkDestroyed(new Vector2Int(cellPos.x, cellPos.y));

        Destroy(gameObject);
    }
}