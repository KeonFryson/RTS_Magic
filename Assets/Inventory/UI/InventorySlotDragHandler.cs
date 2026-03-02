using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    public int SlotIndex { get; set; }

    private InventoryUI inventoryUI;
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
        canvas = GetComponentInParent<Canvas>();

        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;

        if (!inventoryUI.HasItem(SlotIndex))
        {
            Debug.Log($"[InventorySlotDragHandler] No item in slot {SlotIndex}, swap not allowed.");
            return;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!inventoryUI.HasItem(SlotIndex))
        {
            Debug.Log($"[InventorySlotDragHandler] No item in slot {SlotIndex}, swap not allowed.");
            return;
        }
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;
        transform.SetAsLastSibling();

        layoutElement.ignoreLayout = true;
        layoutElement.layoutPriority = 100;

        // Ensure the slot is rendered above others
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
        if (!inventoryUI.HasItem(SlotIndex))
        {
            Debug.Log($"[InventorySlotDragHandler] No item in slot {SlotIndex}, swap not allowed.");
            return;
        }
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Restore raycast and visual state
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Restore layout state
        layoutElement.ignoreLayout = false;
        layoutElement.layoutPriority = 1;

        // Restore canvas sorting
        if (dragCanvas != null)
        {
            dragCanvas.sortingOrder = originalSortingOrder;
            if (canvasOverridden)
            {
                dragCanvas.overrideSorting = false;
                // If we added the Canvas for drag, remove it after drag ends
                if (dragCanvas.gameObject == gameObject)
                {
                    Destroy(dragCanvas);
                }
            }
        }

        // Reset position
        rectTransform.anchoredPosition = originalPosition;

        bool swapped = false;

        GameObject targetObj = eventData.pointerEnter;
        if (targetObj != null)
        {
            var componentNames = string.Join(", ", targetObj.GetComponents<Component>().Select(c => c.GetType().Name));
        }
        else
        {
            Debug.Log("[InventorySlotDragHandler] pointerEnter GameObject: null");
        }

        // Traverse up the hierarchy to find InventorySlotDragHandler
        InventorySlotDragHandler targetHandler = null;
        Transform current = targetObj != null ? targetObj.transform : null;
        while (current != null)
        {
            targetHandler = current.GetComponent<InventorySlotDragHandler>();
            if (targetHandler != null && current.gameObject != gameObject)
                break;
            current = current.parent;
        }

        // Check if this slot has an item before allowing swap
        if (targetHandler != null && targetHandler != this)
        {
            if (!inventoryUI.HasItem(SlotIndex))
            {
                Debug.Log($"[InventorySlotDragHandler] No item in slot {SlotIndex}, swap not allowed.");
            }
            else
            {
                inventoryUI.SwapItems(SlotIndex, targetHandler.SlotIndex);
                swapped = true;
            }
        }

        if (!swapped)
        {
            Debug.Log($"[InventorySlotDragHandler] No swap detected, resetting position for slot {SlotIndex}");
        }
        else
        {
            Debug.Log($"[InventorySlotDragHandler] Swap complete, resetting position for slot {SlotIndex}");
        }
    }
}