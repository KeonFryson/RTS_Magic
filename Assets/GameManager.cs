using UnityEditor;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        SaveManager.AllowEditorSave = true;
        SaveManager.LoadAll();
    }


    void Start()
    {

        foreach (var data in SaveManager.GetPlacedBuildings())
        {
            Debug.Log($"Loading building: itemID={data.itemID}, pos=({data.x},{data.y},{data.z})");
            InventoryItem item = ItemDatabase.Instance.GetInventoryItemByID(data.itemID);
            if (item == null)
            {
                Debug.LogWarning($"Item not found for itemID: {data.itemID}");
                continue;
            }
            if (item.itemData.buildingPrefab == null)
            {
                Debug.LogWarning($"No buildingPrefab for itemID: {data.itemID}");
                continue;
            }
            Vector3 pos = new Vector3(data.x, data.y, data.z);
            GameObject.Instantiate(item.itemData.buildingPrefab, pos, Quaternion.identity);
            Debug.Log($"Instantiated buildingPrefab for itemID: {data.itemID} at {pos}");
        }
       

        var player = GameObject.FindWithTag("Player");
        var playerpos = SaveManager.LoadPlayerPosition();
        if (player != null && playerpos.HasValue)
        {
            player.transform.position = playerpos.Value;
            Debug.Log($"Player position loaded: {playerpos.Value}");
        }

        SaveManager.LoadAllStorageBoxes();

    }

    private void OnApplicationQuit()
    {
        if (!SaveManager.AllowEditorSave)
        {
            Debug.LogWarning("Editor save is disabled. Skipping save on application quit.");
            return;
        }
        
        SaveManager.SavePlacedBuildings();
        SaveManager.SaveAllStorageBoxes();
        SaveManager.SaveAll();
        
    }

    // Optionally, call SaveManager.SaveAll() at other times (e.g., on pause, manual save, etc.)

    [ContextMenu("RESET ALL SAVES")]
    private void ResetAllSaves()
    {
        SaveManager.AllowEditorSave = false; // Disable editor save to prevent saving after reset
        PlayerPrefs.DeleteAll();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        Debug.Log("All saves have been reset.");
    }
}