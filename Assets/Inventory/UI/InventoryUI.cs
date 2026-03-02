using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] public GameObject itemSlotPrefab;

    private int selectedItem = 0;
    [SerializeField] private Outline[] itemOutlineSlots;
    [SerializeField] public GameObject[] itemSlotObjects;
    [SerializeField] public GameObject SlotsParent;


    void Start()
    {
        itemSlotObjects = new GameObject[Inventory.Instance.MaxItems];
        for (int i = 0; i < Inventory.Instance.MaxItems; i++)
        {
            GameObject slot = Instantiate(itemSlotPrefab, SlotsParent.transform);
            slot.name = $"ItemSlot_{i}";
            itemSlotObjects[i] = slot;

            GameObject slotContent = new GameObject("SlotContent", typeof(RectTransform));
            slotContent.transform.SetParent(slot.transform, false);


            var image = slot.GetComponentInChildren<Image>();
            var quantityText = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            if (image != null)
            {
                image.transform.SetParent(slotContent.transform, true);
               
            }

            if (quantityText != null)
            {
                quantityText.transform.SetParent(slotContent.transform, true);
            }

            var dragHandler = slotContent.AddComponent<UniversalSlotDragHandler>();
            dragHandler.SlotIndex = i;
            dragHandler.inventoryUI = this;

        }

        itemOutlineSlots = GetComponentsInChildren<Outline>();
        itemOutlineSlots[selectedItem].enabled = true;

        Inventory.Instance.OnInventoryChanged += UpdateInventoryUI;
        UpdateInventoryUI();
    }
    public bool HasItem(int slotIndex)
    {
        InventoryItem[] items = Inventory.Instance.GetItems();
        return slotIndex >= 0 && slotIndex < items.Length && items[slotIndex] != null;
    }

    void Update()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll != 0) // Only update if scrolling
        {
            // Deselect old
            if (selectedItem >= 0 && selectedItem < itemOutlineSlots.Length)
                itemOutlineSlots[selectedItem].enabled = false;

            if (scroll > 0) // Scroll Up
                selectedItem = (selectedItem - 1 + itemOutlineSlots.Length) % itemOutlineSlots.Length;
            else // Scroll Down
               selectedItem = (selectedItem + 1) % itemOutlineSlots.Length;
           

            // Select new
            if (selectedItem >= 0 && selectedItem < itemOutlineSlots.Length)
                itemOutlineSlots[selectedItem].enabled = true;
        }
    }

    public void SwapItems(int indexA, int indexB)
    {
        Inventory.Instance.MoveItem(indexA, indexB);
        UpdateInventoryUI();
    }

    public void UpdateInventoryUI()
    {
        InventoryItem[] items = Inventory.Instance.GetItems();
        for (int i = 0; i < itemSlotObjects.Length; i++)
        {
            // Update SlotIndex for drag handler
            var dragHandler = itemSlotObjects[i].GetComponent<UniversalSlotDragHandler>();
            if (dragHandler != null)
                dragHandler.SlotIndex = i;

            Image itemImage = itemSlotObjects[i].GetComponentInChildren<Image>();
            TMPro.TextMeshProUGUI quantityText = itemSlotObjects[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (items[i] != null)
            {
                itemImage.sprite = items[i].itemData.itemIcon;
                quantityText.text = items[i].itemData.quantity > 1 ? items[i].itemData.quantity.ToString() : "";
            }
            else
            {
                itemImage.sprite = null;
                quantityText.text = "";
            }
        }
    }

    public int GetSelectedItemIndex()
    {
        return selectedItem;
    }



}
