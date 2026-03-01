using UnityEngine;

public enum RecipeCategory
{
    Parts,
    Buildings,
    Weapons
}

[CreateAssetMenu(menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public RecipeCategory category;
    public InventoryItem[] ingredients;
    public int[] ingredientCounts;
    public InventoryItem result;
    public int resultCount = 1;
}