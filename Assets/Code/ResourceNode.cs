using UnityEngine;
using System.Collections;

public class ResourceNode : MonoBehaviour
{
    [SerializeField] private InventoryItem resourceItem;
    [SerializeField] public int minquantity = 1;
    [SerializeField] public int maxquantity = 5;
    [SerializeField] public int Heath = 10;
    [SerializeField] public ParticleSystem DamageEffect;

    private Vector3 originalPosition;

    public void TakeDamage(int damage)
    {
        Heath -= damage;
        Vector2 pos = new Vector2(transform.position.x, transform.position.y + 1.1f);
        Instantiate(DamageEffect, pos, Quaternion.Euler(90,0,0));
        StartCoroutine(Shake(0.15f, 0.2f)); // duration, magnitude
        if (Heath <= 0)
        {
            Harvest();
        }
    }

    private IEnumerator Shake(float duration, float magnitude)
    {
        originalPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;
            transform.position = originalPosition + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;
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
        SaveManager.MarkResourceNodeDestroyed(new Vector2Int(cellPos.x, cellPos.y));

        Destroy(gameObject);
    }
}