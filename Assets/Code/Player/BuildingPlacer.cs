using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BuildingPlacer : MonoBehaviour
{
    [Header("Placement Settings")]
    public LayerMask buildingLayerMask = ~0; // All layers by default

    private GameObject previewInstance;
    private InventoryItem selectedBuildingItem;
    private bool isPlacing = false;
    private PlayerInputs playerInputs;

    private void Start()
    {
        playerInputs = PlayerInputs.Instance;
        if (playerInputs != null)
        {
            playerInputs.OnClick += HandleClick;
            playerInputs.OnRightClick += HandleRightClick;
        }
    }

    private void OnDestroy()
    {
        if (playerInputs != null)
        {
            playerInputs.OnClick -= HandleClick;
            playerInputs.OnRightClick -= HandleRightClick;
        }
    }

    private void Update()
    {
        // Block placement if mouse is over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1))
            return;

        // Check inventory selection each frame
        InventoryItem invItem = GetSelectedBuildingItem();
        if (invItem != selectedBuildingItem)
        {
            CancelPlacement();
            selectedBuildingItem = invItem;
            if (selectedBuildingItem != null && selectedBuildingItem.itemData.buildingPrefab != null)
                StartPlacing(selectedBuildingItem);
        }

        if (!isPlacing || selectedBuildingItem == null || selectedBuildingItem.itemData.buildingPrefab == null)
            return;

        Vector3 mouseWorld = GetMouseWorldPosition();
        Vector3Int cellPos = WorldGen.Instance.tilemap.WorldToCell(mouseWorld);

        // Snap preview to cell center
        Vector3 cellCenter = WorldGen.Instance.tilemap.GetCellCenterWorld(cellPos);

        if (previewInstance == null)
        {
            previewInstance = Instantiate(selectedBuildingItem.itemData.buildingPrefab, cellCenter, Quaternion.identity);
            SetPreviewMaterial(previewInstance, true);
        }
        else
        {
            previewInstance.transform.position = cellCenter;
        }
    }

    private void HandleClick(Vector3 worldPos)
    {
        if (!isPlacing || selectedBuildingItem == null || selectedBuildingItem.itemData.buildingPrefab == null)
            return;

        Vector3Int cellPos = WorldGen.Instance.tilemap.WorldToCell(worldPos);
        Vector3 cellCenter = WorldGen.Instance.tilemap.GetCellCenterWorld(cellPos);

        if (CanPlaceBuilding(cellPos))
        {
            PlaceBuilding(cellCenter);
        }
    }

    private void HandleRightClick(Vector3 worldPos)
    {
        if (isPlacing)
        {
            CancelPlacement();
        }
    }

    private InventoryItem GetSelectedBuildingItem()
    {
        int selectedIndex = FindFirstObjectByType<InventoryUI>().GetSelectedItemIndex();
        InventoryItem[] items = Inventory.Instance.GetItems();
        if (selectedIndex >= 0 && selectedIndex < items.Length)
        {
            InventoryItem item = items[selectedIndex];
            if (item != null && item.itemData.buildingPrefab != null)
                return item;
        }
        return null;
    }

    private void StartPlacing(InventoryItem buildingItem)
    {
        isPlacing = true;
        selectedBuildingItem = buildingItem;
    }

    private void PlaceBuilding(Vector3 position)
    {
        GameObject placed = Instantiate(selectedBuildingItem.itemData.buildingPrefab, position, Quaternion.identity);

        

        SaveManager.AddPlacedBuilding(selectedBuildingItem.itemData.itemID, position);
        // Optionally: Remove one building item from inventory
        Inventory.Instance.RemoveItemStack(selectedBuildingItem.itemData.itemName, 1);
        CancelPlacement();
    }

    private void CancelPlacement()
    {
        isPlacing = false;
        if (previewInstance != null)
            Destroy(previewInstance);
        previewInstance = null;
        selectedBuildingItem = null;
    }

    private bool CanPlaceBuilding(Vector3Int cellPos)
    {
        if (!WorldGen.Instance.IsLandTile(new Vector2Int(cellPos.x, cellPos.y)))
            return false;

        // Use the selected building's placement mask if available, otherwise fallback to the default
        LayerMask mask = selectedBuildingItem != null
            ? selectedBuildingItem.itemData.placementMask
            : buildingLayerMask;

        Collider[] colliders = Physics.OverlapBox(
            WorldGen.Instance.tilemap.GetCellCenterWorld(cellPos),
            Vector3.one * 0.4f,
            Quaternion.identity,
            mask
        );
        return colliders.Length == 0;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Camera cam = Camera.main;
        Vector3 mouseWorld = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -cam.transform.position.z));
        mouseWorld.z = 0;
        return mouseWorld;
    }

    private void SetPreviewMaterial(GameObject go, bool isPreview)
    {
        foreach (var renderer in go.GetComponentsInChildren<Renderer>())
        {
            if (isPreview)
                renderer.material.color = new Color(1, 1, 1, 0.5f);
            else
                renderer.material.color = Color.white;
        }
    }
}