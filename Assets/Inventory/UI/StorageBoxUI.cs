using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class StorageBoxUI : MonoBehaviour
{
    public StorageBox storageBox;
    public Transform itemListContainer; // Assign in Inspector
    public GameObject itemEntryPrefab;  // Assign in Inspector

    public GameObject[] itemSlotObjects;

    public void Show(StorageBox box)
    {
        storageBox = box;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // Assets\Inventory\UI\StorageBoxUI.cs
    public void Refresh()
    {
        foreach (Transform child in itemListContainer)
            Destroy(child.gameObject);

        int capacity = storageBox.Capacity;
        var slots = storageBox.GetSlots();

        itemSlotObjects = new GameObject[capacity];

        for (int i = 0; i < capacity; i++)
        {
            GameObject slot = Instantiate(itemEntryPrefab, itemListContainer);
            slot.name = $"StorageSlot_{i}";
            itemSlotObjects[i] = slot;

            GameObject slotContent = new GameObject("SlotContent", typeof(RectTransform));
            slotContent.transform.SetParent(slot.transform, false);

            // Ensure slotContent has an Image for raycast target
            var slotImage = slotContent.AddComponent<Image>();
            slotImage.color = new Color(1, 1, 1, 0); // Fully transparent
            slotImage.raycastTarget = true;

            var image = slot.GetComponentInChildren<Image>();
            var quantityText = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            if (image != null)
                image.transform.SetParent(slotContent.transform, true);

            if (quantityText != null)
                quantityText.transform.SetParent(slotContent.transform, true);

            var dragHandler = slotContent.AddComponent<UniversalSlotDragHandler>();
            dragHandler.SlotIndex = i;
            dragHandler.storageBoxUI = this;

            // Set slot content
            if (slots[i].item != null && slots[i].item.itemData.quantity > 0)
            {
                if (quantityText != null)
                    quantityText.text = slots[i].item.itemData.quantity > 1 ? slots[i].item.itemData.quantity.ToString() : string.Empty;

                if (image != null)
                {
                    image.sprite = slots[i].item.itemData.itemIcon;
                    image.enabled = image.sprite != null;
                }
            }
            else
            {
                if (quantityText != null)
                    quantityText.text = "";
                if (image != null)
                {
                    image.sprite = null;
                    image.enabled = false;
                }
            }
        }
    }

    public void SwapItems(int indexA, int indexB)
    {
        Debug.Log($"Swapping storage box slots {indexA} <-> {indexB}");
        storageBox.SwapSlots(indexA, indexB);
        Refresh();
    }

    // Example method to move item from storage box to player inventory
    public void MoveItemToInventory(int itemId, int amount)
    {
        if (storageBox.RemoveItem(itemId, amount))
        {
            Inventory.Instance.AddItemByID(itemId, amount);
            Refresh();
            // Optionally refresh player inventory UI
        }
    }

    // Example method to move item from player inventory to storage box using InventoryItem
    public void MoveItemFromInventory(int itemId, int amount)
    {
        // Find the item in the inventory
        InventoryItem[] items = Inventory.Instance.GetItems();
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item != null && item.itemData.itemID == itemId && item.itemData.quantity >= amount)
            {
                // Clone the item and set the quantity to move
                InventoryItem newItem = ScriptableObject.CreateInstance<InventoryItem>();
                newItem.itemData = new ItemData
                {
                    itemName = item.itemData.itemName,
                    description = item.itemData.description,
                    itemIcon = item.itemData.itemIcon,
                    itemID = item.itemData.itemID,
                    quantity = amount,
                    maxStackSize = item.itemData.maxStackSize,
                    buildingPrefab = item.itemData.buildingPrefab,
                    placementMask = item.itemData.placementMask,
                };

                // Remove the amount from inventory
                if (Inventory.Instance.RemoveItemByID(itemId, amount))
                {
                    storageBox.AddItem(newItem);
                    Refresh();
                    // Optionally refresh player inventory UI
                }
                break;
            }
        }
    }


}