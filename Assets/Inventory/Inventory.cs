using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    public System.Action OnInventoryChanged;

    [SerializeField] private int maxItems = 10;
    public int MaxItems => maxItems;

    [SerializeField] private InventoryItem[] items;

    [Header("Test Items")]
    public InventoryItem[] TestItem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        items = new InventoryItem[maxItems];

        // Load inventory data after singleton is set
        SaveManager.LoadInventory(this);
    }

    private void Start()
    {
        // Only add test items if inventory is empty (i.e., no save loaded)
        bool hasAnyItem = false;
        foreach (var item in items)
        {
            if (item != null)
            {
                hasAnyItem = true;
                break;
            }
        }

        if (!hasAnyItem)
        {
            for (int i = 0; i < TestItem.Length; i++)
            {
                AddItem(TestItem[i]);
            }
        }
    }




    public InventoryItem CreateSampleItem(string name, string description, int quantity)
    {
        InventoryItem newItem = ScriptableObject.CreateInstance<InventoryItem>();
        newItem.name = name;
        newItem.itemData = new ItemData { itemName = name, description = description, quantity = quantity };
        Debug.Log($"Created sample item: {name}");
        return newItem;
    }

    public void AddItem(InventoryItem item)
    {
        int maxStack = item.itemData.maxStackSize;
        int quantityToAdd = item.itemData.quantity;

        // Try to add to existing stacks first
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].itemData.itemName == item.itemData.itemName)
            {
                int currentQuantity = items[i].itemData.quantity;
                if (currentQuantity < maxStack)
                {
                    int space = maxStack - currentQuantity;
                    int addAmount = Mathf.Min(space, quantityToAdd);
                    items[i].itemData.quantity += addAmount;
                    quantityToAdd -= addAmount;
                    OnInventoryChanged?.Invoke();
                    Debug.Log($"Stacked {addAmount} {item.itemData.itemName} to slot {i}.");

                    if (quantityToAdd <= 0)
                        return;
                }
            }
        }

        // Add new stacks if needed
        while (quantityToAdd > 0)
        {
            int emptyIndex = -1;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    emptyIndex = i;
                    break;
                }
            }

            if (emptyIndex == -1)
            {
                Debug.LogWarning("Inventory is full!");
                return;
            }

            int addAmount = Mathf.Min(maxStack, quantityToAdd);
            InventoryItem newStack = ScriptableObject.CreateInstance<InventoryItem>();
            newStack.itemData = new ItemData
            {
                itemName = item.itemData.itemName,
                description = item.itemData.description,
                itemIcon = item.itemData.itemIcon,
                quantity = addAmount,
                maxStackSize = maxStack
            };
            items[emptyIndex] = newStack;
            quantityToAdd -= addAmount;
            OnInventoryChanged?.Invoke();
            Debug.Log($"Created new stack of {addAmount} {item.itemData.itemName} in slot {emptyIndex}.");
        }
    }
    public void RemoveItemStack(string itemName, int quantity)
    {
        int quantityToRemove = quantity;

        for (int i = 0; i < items.Length && quantityToRemove > 0; i++)
        {
            if (items[i] != null && items[i].itemData.itemName == itemName)
            {
                int stackQuantity = items[i].itemData.quantity;
                if (stackQuantity > quantityToRemove)
                {
                    items[i].itemData.quantity -= quantityToRemove;
                    quantityToRemove = 0;
                    Debug.Log($"Removed {quantity} {itemName} from slot {i}. Remaining in stack: {items[i].itemData.quantity}");
                }
                else
                {
                    quantityToRemove -= stackQuantity;
                    Debug.Log($"Removed {stackQuantity} {itemName} from slot {i}. Stack emptied.");
                    items[i] = null;
                }
                OnInventoryChanged?.Invoke();
            }
        }

        if (quantityToRemove > 0)
            Debug.LogWarning($"Not enough {itemName} in inventory to remove {quantity}.");
    }

    public void RemoveItem(InventoryItem item)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == item)
            {
                items[i] = null;
                OnInventoryChanged?.Invoke();
                Debug.Log($"Removed {item.itemData.itemName} from inventory.");
                return;
            }
        }
        Debug.LogWarning("Item not found in inventory!");
    }


    public void MoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= items.Length || toIndex < 0 || toIndex >= items.Length)
        {
            Debug.LogWarning("Invalid inventory indices!");
            return;
        }
        InventoryItem temp = items[fromIndex];
        items[fromIndex] = items[toIndex];
        items[toIndex] = temp;
        OnInventoryChanged?.Invoke();
        Debug.Log($"Moved item from slot {fromIndex} to slot {toIndex}.");
    }


    public void UseItem(InventoryItem item)
    {
        // Implement item usage logic here
        OnInventoryChanged?.Invoke();
        Debug.Log($"Used {item.itemData.itemName}.");
    }

    public void ClearInventory()
    {
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = null;
            OnInventoryChanged?.Invoke();
        }
        Debug.Log("Inventory cleared.");
    }

    public InventoryItem[] GetItems()
    {
        return items;
    }




}
