using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UniversalSlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    public int SlotIndex { get; set; }
    public InventoryUI inventoryUI;
    public StorageBoxUI storageBoxUI;

    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;
    private Canvas dragCanvas;
    private Vector2 originalPosition;
    private int originalSortingOrder;
    private bool canvasOverridden;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        inventoryUI = GetComponentInParent<InventoryUI>();
        storageBoxUI = GetComponentInParent<StorageBoxUI>();
        canvas = GetComponentInParent<Canvas>();

        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        Debug.Log($"Pointer down on slot {SlotIndex} at position {originalPosition}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;
        transform.SetAsLastSibling();

        layoutElement.ignoreLayout = true;
        layoutElement.layoutPriority = 100;

        dragCanvas = GetComponent<Canvas>();
        if (dragCanvas == null)
        {
            dragCanvas = gameObject.AddComponent<Canvas>();
            canvasOverridden = true;
        }
        else
        {
            canvasOverridden = !dragCanvas.overrideSorting;
        }
        originalSortingOrder = dragCanvas.sortingOrder;
        dragCanvas.overrideSorting = true;
        dragCanvas.sortingOrder = 1000;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        layoutElement.ignoreLayout = false;
        layoutElement.layoutPriority = 1;

        if (dragCanvas != null)
        {
            dragCanvas.sortingOrder = originalSortingOrder;
            if (canvasOverridden)
            {
                dragCanvas.overrideSorting = false;
                if (dragCanvas.gameObject == gameObject)
                {
                    Destroy(dragCanvas);
                }
            }
        }

        rectTransform.anchoredPosition = originalPosition;

        GameObject targetObj = eventData.pointerEnter;
        UniversalSlotDragHandler targetHandler = null;
        Transform current = targetObj != null ? targetObj.transform : null;
        while (current != null)
        {
            targetHandler = current.GetComponent<UniversalSlotDragHandler>();
            if (targetHandler != null && current.gameObject != gameObject)
                break;
            current = current.parent;
        }

        if (targetHandler != null && targetHandler != this)
        {
            // Inventory to Inventory
            if (inventoryUI != null && targetHandler.inventoryUI != null)
            {
                inventoryUI.SwapItems(SlotIndex, targetHandler.SlotIndex);
            }
            // StorageBox to StorageBox
            else if (storageBoxUI != null && targetHandler.storageBoxUI != null)
            {
                storageBoxUI.SwapItems(SlotIndex, targetHandler.SlotIndex);
            }
            // Inventory to StorageBox
            else if (storageBoxUI != null && targetHandler.inventoryUI != null)
            {
                var slots = storageBoxUI.storageBox.GetSlots();
                if (SlotIndex >= 0 && SlotIndex < slots.Count && slots[SlotIndex].item != null)
                {
                    var item = slots[SlotIndex].item;
                    int itemId = item.itemData.itemID;
                    int amount = item.itemData.quantity;
                    if (storageBoxUI.storageBox.RemoveItem(itemId, amount))
                    {
                        Inventory.Instance.AddItem(item, amount);
                        storageBoxUI.Refresh();
                        targetHandler.inventoryUI.UpdateInventoryUI();
                    }
                }
            }
            else if (inventoryUI != null && targetHandler.storageBoxUI != null)
            {
                InventoryItem[] items = Inventory.Instance.GetItems();
                if (SlotIndex >= 0 && SlotIndex < items.Length && items[SlotIndex] != null)
                {
                    var item = items[SlotIndex];
                    int itemId = item.itemData.itemID;
                    int amount = item.itemData.quantity;
                    if (Inventory.Instance.RemoveItemByID(itemId, amount))
                    {
                        targetHandler.storageBoxUI.storageBox.AddItem(item);
                        inventoryUI.UpdateInventoryUI();
                        targetHandler.storageBoxUI.Refresh();
                    }
                }
            }
        }
    }
}