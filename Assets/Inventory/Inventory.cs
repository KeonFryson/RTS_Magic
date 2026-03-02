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

    [ContextMenu("Add Sample Item")]
    public void AddSampleItem()
    {

        AddItem(TestItem[0]);
        Debug.Log("Added sample item to inventory.");
    }

    public bool RemoveItemByID(int itemID, int quantity)
    {
        int quantityToRemove = quantity;
        for (int i = 0; i < items.Length && quantityToRemove > 0; i++)
        {
            if (items[i] != null && items[i].itemData.itemID == itemID)
            {
                int stackQuantity = items[i].itemData.quantity;
                if (stackQuantity > quantityToRemove)
                {
                    items[i].itemData.quantity -= quantityToRemove;
                    quantityToRemove = 0;
                }
                else
                {
                    quantityToRemove -= stackQuantity;
                    items[i] = null;
                }
                OnInventoryChanged?.Invoke();
            }
        }
        if (quantityToRemove > 0)
        {
            Debug.LogWarning($"Not enough of itemID {itemID} in inventory to remove {quantity}.");
            return false;
        }
        return true;
    }

    public InventoryItem CreateSampleItem(string name, string description, int quantity)
    {
        InventoryItem newItem = ScriptableObject.CreateInstance<InventoryItem>();
        newItem.name = name;
        newItem.itemData = new ItemData { itemName = name, description = description, quantity = quantity };
        Debug.Log($"Created sample item: {name}");
        return newItem;
    }

    // Returns the total quantity of items with the given itemID
    public int GetItemCount(int itemID)
    {
        int count = 0;
        foreach (var item in items)
        {
            if (item != null && item.itemData.itemID == itemID)
            {
                count += item.itemData.quantity;
            }
        }
        return count;
    }

    // Adds a specific quantity of an item (used by crafting)
    public void AddItem(InventoryItem item, int quantity)
    {
        int originalQuantity = item.itemData.quantity;
        item.itemData.quantity = quantity;
        AddItem(item);
        item.itemData.quantity = originalQuantity; // Restore original in case it's reused elsewhere
    }
    public void AddItemByID(int itemID, int quantity)
    {
        // Use ItemDatabase to get the item template by ID
        InventoryItem template = ItemDatabase.Instance != null
            ? ItemDatabase.Instance.GetInventoryItemByID(itemID)
            : null;

        if (template == null)
        {
            Debug.LogWarning($"Item ID {itemID} not found in ItemDatabase.");
            return;
        }

        // Create a new InventoryItem instance for stacking logic
        InventoryItem newItem = ScriptableObject.CreateInstance<InventoryItem>();
        newItem.itemData = new ItemData
        {
            itemName = template.itemData.itemName,
            description = template.itemData.description,
            itemIcon = template.itemData.itemIcon,
            itemID = template.itemData.itemID,
            quantity = quantity,
            maxStackSize = template.itemData.maxStackSize,
            buildingPrefab = template.itemData.buildingPrefab,
            placementMask = template.itemData.placementMask,
        };

        AddItem(newItem);
    }

    // Removes a specific quantity of an item by itemID (used by crafting)
    public void RemoveItem(int itemID, int quantity)
    {
        int quantityToRemove = quantity;
        for (int i = 0; i < items.Length && quantityToRemove > 0; i++)
        {
            if (items[i] != null && items[i].itemData.itemID == itemID)
            {
                int stackQuantity = items[i].itemData.quantity;
                if (stackQuantity > quantityToRemove)
                {
                    items[i].itemData.quantity -= quantityToRemove;
                    quantityToRemove = 0;
                }
                else
                {
                    quantityToRemove -= stackQuantity;
                    items[i] = null;
                }
                OnInventoryChanged?.Invoke();
            }
        }
        if (quantityToRemove > 0)
            Debug.LogWarning($"Not enough of itemID {itemID} in inventory to remove {quantity}.");
    }


    // Adds an item to the inventory, stacking if possible
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
                itemID = item.itemData.itemID,
                quantity = addAmount,
                maxStackSize = maxStack,
                buildingPrefab = item.itemData.buildingPrefab,
                placementMask = item.itemData.placementMask,

            };
            items[emptyIndex] = newStack;
            quantityToAdd -= addAmount;
            OnInventoryChanged?.Invoke();
         //   Debug.Log($"Created new stack of {addAmount} {item.itemData.itemName} in slot {emptyIndex}.");
        }
    }

    // Removes a specific quantity of an item by name (legacy, not used by crafting)
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

    // Removes a specific InventoryItem instance
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

    // Moves an item from one slot to another
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

    // Use an item (implement your own logic)
    public void UseItem(InventoryItem item)
    {
        // Implement item usage logic here
        OnInventoryChanged?.Invoke();
        Debug.Log($"Used {item.itemData.itemName}.");
    }

    // Clears the inventory
    public void ClearInventory()
    {
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = null;
            OnInventoryChanged?.Invoke();
        }
       // Debug.Log("Inventory cleared.");
    }

    // Returns all items in the inventory
    public InventoryItem[] GetItems()
    {
        
        return items;
    }

}