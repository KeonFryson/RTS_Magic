using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] public GameObject itemSlotPrefab;

    private int selectedItem = 0;
    [SerializeField] private Outline[] itemOutlineSlots;
    [SerializeField] private GameObject[] itemSlotObjects;
    [SerializeField] public GameObject SlotsParent;


    void Start()
    {
        itemSlotObjects = new GameObject[Inventory.Instance.MaxItems];
        for (int i = 0; i < Inventory.Instance.MaxItems; i++)
        {
            GameObject slot = Instantiate(itemSlotPrefab, SlotsParent.transform);
          
            slot.name = $"ItemSlot_{i}";
            itemSlotObjects[i] = slot;
        }

        itemOutlineSlots = GetComponentsInChildren<Outline>();
        itemOutlineSlots[selectedItem].enabled = true;

        Inventory.Instance.OnInventoryChanged += UpdateInventoryUI;

        // Force initial UI update to reflect loaded inventory
        UpdateInventoryUI();
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


    public void UpdateInventoryUI()
    {
        InventoryItem[] items = Inventory.Instance.GetItems();
        for (int i = 0; i < itemSlotObjects.Length; i++)
        {
            Image itemImage = itemSlotObjects[i].GetComponentInChildren<Image>();
            TMPro.TextMeshProUGUI quantityText = itemSlotObjects[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (items[i] != null)
            {
                itemImage.sprite = items[i].itemData.itemIcon;
                quantityText.text = items[i].itemData.quantity > 1 ? items[i].itemData.quantity.ToString() : "";
                Debug.Log($"Updated slot {i} with {items[i].itemData.itemName} x{items[i].itemData.quantity}");

            }
            else
            {
                itemImage.sprite = null;
                quantityText.text = "";
                Debug.Log($"Cleared slot {i}");
            }
        }
    }

    public int GetSelectedItemIndex()
    {
        return selectedItem;
    }



}
