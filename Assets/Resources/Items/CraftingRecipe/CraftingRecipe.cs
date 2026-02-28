using UnityEngine;

[CreateAssetMenu(menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    public InventoryItem[] ingredients;
    public int[] ingredientCounts;
    public InventoryItem result;
    public int resultCount = 1;
}