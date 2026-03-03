using UnityEngine;
using UnityEngine.InputSystem;

public class UIMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject craftingMenu;
    [SerializeField] private GameObject ChatMwnu;
    [SerializeField] private GameObject storageInventoryMenu;

    private PlayerInputs playerInputs;

    private void Awake()
    {
        playerInputs = PlayerInputs.Instance;
        if (playerInputs != null)
        {
            playerInputs.OnPauseMenu += HandlePauseMenu;
            playerInputs.OnCraftingMenu += HandleCraftingMenu;
            playerInputs.OnStorageMenu += HandleStorageMenu;
            playerInputs.OnChatMenu += HandleChatMenu;
        }
        craftingMenu.SetActive(false);
        pauseMenu.SetActive(false);
        storageInventoryMenu.SetActive(false);
        ChatMwnu.SetActive(false);
    }

    private void OnDestroy()
    {
        if (playerInputs != null)
        {
            playerInputs.OnPauseMenu -= HandlePauseMenu;
            playerInputs.OnCraftingMenu -= HandleCraftingMenu;
            playerInputs.OnStorageMenu -= HandleStorageMenu;
            playerInputs.OnChatMenu -= HandleChatMenu;
        }
    }

    private void HandlePauseMenu() => ToggleMenu(pauseMenu);
    private void HandleCraftingMenu() => ToggleMenu(craftingMenu);
    private void HandleStorageMenu() => ToggleMenu(storageInventoryMenu);
    private void HandleChatMenu() => ToggleMenu(ChatMwnu);

    private void ToggleMenu(GameObject menuToToggle)
    {
        bool willBeActive = !menuToToggle.activeSelf;
        pauseMenu.SetActive(false);
        craftingMenu.SetActive(false);
        storageInventoryMenu.SetActive(false);
        ChatMwnu.SetActive(false);
        if (willBeActive)
        {
            menuToToggle.SetActive(true);
        }
    }
}