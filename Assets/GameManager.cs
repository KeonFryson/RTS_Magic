using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        SaveManager.AllowEditorSave = true;
        SaveManager.LoadAll();
    }

    private void OnApplicationQuit()
    {
        if (!SaveManager.AllowEditorSave)
        {
            Debug.LogWarning("Editor save is disabled. Skipping save on application quit.");
            return;
        }
        
        SaveManager.SaveAll();
    }

    // Optionally, call SaveManager.SaveAll() at other times (e.g., on pause, manual save, etc.)

    [ContextMenu("RESET ALL SAVES")]
    private void ResetAllSaves()
    {
        SaveManager.AllowEditorSave = false; // Disable editor save to prevent saving after reset
        PlayerPrefs.DeleteAll();
        Application.Quit();
        Debug.Log("All saves have been reset.");
    }
}