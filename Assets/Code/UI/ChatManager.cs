using UnityEngine;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;

public class ChatManager : MonoBehaviour
{
    public TMPro.TMP_InputField chatInputField; // Assign in Inspector
    public TMPro.TextMeshProUGUI chatHistoryText; // Assign in Inspector

    private const int MaxChatLines = 10;
    private readonly List<string> chatLines = new List<string>();

    private void Start()
    {
        chatInputField.onEndEdit.AddListener(OnChatSubmitted);
    }

    private void OnChatSubmitted(string input)
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (!string.IsNullOrWhiteSpace(input))
            {
                if (input.StartsWith("/"))
                {
                    HandleCommand(input.Substring(1));
                }
                else
                {
                    AddMessage($"<color=yellow>You:</color> {input}");
                }
            }
            chatInputField.text = string.Empty;
            chatInputField.ActivateInputField();
        }
    }

    private void AddMessage(string message)
    {
        chatLines.Add(message);
        if (chatLines.Count > MaxChatLines)
            chatLines.RemoveAt(0);

        var sb = new StringBuilder();
        foreach (var line in chatLines)
            sb.AppendLine(line);

        chatHistoryText.text = sb.ToString();
    }

    private void HandleCommand(string commandLine)
    {
        string[] parts = commandLine.Split(' ');
        string command = parts[0].ToLower();

        switch (command)
        {
            case "hello":
                AddMessage("<color=green>System:</color> Hello, player!");
                break;
            case "give":
                if (parts.Length >= 3)
                {
                    int itemId, amount;
                    if (int.TryParse(parts[1], out itemId) && int.TryParse(parts[2], out amount))
                    {
                        var itemData = ItemDatabase.Instance.GetInventoryItemByID(itemId);
                        if (itemData != null)
                        {
                            Inventory.Instance.AddItemByID(itemId, amount);
                            AddMessage($"<color=green>System:</color> Gave you {amount} of item ID '{itemId}'.");
                        }
                        else
                        {
                            AddMessage($"<color=red>System:</color> Item ID '{itemId}' not found.");
                        }
                    }
                    else
                    {
                        AddMessage("<color=red>System:</color> Invalid item ID or amount.");
                    }
                }
                else
                {
                    AddMessage("<color=red>System:</color> Usage: /give <itemID> <amount>");
                }
                break;
            case "help":
                AddMessage("<color=green>System:</color> Available commands:");
                AddMessage("<color=green>System:</color> /hello - Greet the system.");
                AddMessage("<color=green>System:</color> /give <itemID> <amount> - Add items to your inventory.");
                AddMessage("<color=green>System:</color> /help - Show this help message.");
                break;
            default:
                AddMessage($"<color=red>System:</color> Unknown command: {command}");
                break;
        }
    }
}