using UnityEngine;
using UnityEngine.InputSystem;

public class UIMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject craftingMenu;
    [SerializeField] private GameObject ChatMwnu;
    [SerializeField] private GameObject storageInventoryMenu;

    private InputSystem_Actions inputActions;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
        craftingMenu.SetActive(false);
        pauseMenu.SetActive(false);
        storageInventoryMenu.SetActive(false);
        ChatMwnu.SetActive(false);
    }

    private void OnDestroy()
    { 
        inputActions.Disable();
    }

    private void Update()
    {
        if(Keyboard.current.escapeKey.wasPressedThisFrame && pauseMenu != null)
        {
            ToggleMenu(pauseMenu);
            Debug.Log("Pause menu toggled");
        }
        else if (Keyboard.current.cKey.wasPressedThisFrame && craftingMenu != null)
        {
            ToggleMenu(craftingMenu);
            Debug.Log("Crafting menu toggled");
        }
        else if (Keyboard.current.leftBracketKey.wasPressedThisFrame && storageInventoryMenu != null)
        {
            ToggleMenu(storageInventoryMenu);
        }
        else if (Keyboard.current.tabKey.wasPressedThisFrame && ChatMwnu != null)
        {
            ToggleMenu(ChatMwnu);
        }
    }

    private void ToggleMenu(GameObject menuToToggle)
    {
        bool willBeActive = !menuToToggle.activeSelf;

        // Deactivate all menus first
        pauseMenu.SetActive(false);
        craftingMenu.SetActive(false);
        storageInventoryMenu.SetActive(false);
        ChatMwnu.SetActive(false);

        // Activate the selected menu if it was not already active
        if (willBeActive)
        {
            menuToToggle.SetActive(true);
        }
    }
}