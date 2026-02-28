using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }
    public List<CraftingRecipe> recipes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool CanCraft(CraftingRecipe recipe, Inventory inventory)
    {
        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            if (inventory.GetItemCount(recipe.ingredients[i].itemData.itemID) < recipe.ingredientCounts[i])
                return false;
        }
        return true;
    }

    public void Craft(CraftingRecipe recipe, Inventory inventory)
    {
         
        if (!CanCraft(recipe, inventory))
            return;

        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            inventory.RemoveItem(recipe.ingredients[i].itemData.itemID, recipe.ingredientCounts[i]);
        }
        inventory.AddItem(recipe.result, recipe.resultCount);
    }
}