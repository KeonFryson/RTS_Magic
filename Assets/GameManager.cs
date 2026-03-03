using UnityEditor;
using UnityEngine;
using System.IO;

public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        SaveManager.AllowEditorSave = true;
        SaveManager.LoadAll();
    }

    private void Start()
    {
        LoadGame();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    [ContextMenu("SAVE GAME")]
    private void SaveGame()
    {
        SaveManager.SaveAll();
        Debug.Log("Game saved.");
    }

    [ContextMenu("LOAD GAME")]
    private void LoadGame()
    {
        SaveManager.LoadAll();
         
        Debug.Log("Game loaded.");
    }

    [ContextMenu("RESET ALL SAVES")]
    private void ResetAllSaves()
    {
        SaveManager.AllowEditorSave = false;
        string saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        Debug.Log("All saves have been reset.");
    }
}