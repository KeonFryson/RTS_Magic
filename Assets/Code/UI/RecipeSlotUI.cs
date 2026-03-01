using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class RecipeSlotUI : MonoBehaviour
{
    public Button button;
    public Image icon;
    public CraftingRecipe CurrentRecipe { get; set; }



    private static readonly Color NormalColor = Color.white;
    private static readonly Color HighlightedColor = new Color(0.7f, 0.7f, 0.7f); // Gray
    private static readonly Color PressedColor = new Color(0.85f, 0.85f, 0.85f); // Light gray
    private static readonly Color SelectedColor = new Color(0.7f, 0.7f, 0.7f); // Gray
    private static readonly Color DisabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    public void SetRecipe(CraftingRecipe recipe)
    {
        if (icon != null && recipe.result != null && recipe.result.itemData.itemIcon != null)
            icon.sprite = recipe.result.itemData.itemIcon;
        // Set button color states
 
        var colors = button.colors;
        colors.normalColor = NormalColor;
        colors.highlightedColor = HighlightedColor;
        colors.pressedColor = PressedColor;
        colors.selectedColor = SelectedColor;
        colors.disabledColor = DisabledColor;
        button.colors = colors;

        // Ensure transition is set to ColorTint
        button.transition = Selectable.Transition.ColorTint;
    }

    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
    }
}