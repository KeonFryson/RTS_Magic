using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CarouselSlot : MonoBehaviour
{
    public Image resultImage;
    public Outline resultOutline;
   // public Text resultName;
    //public Text ingredientsText;

    public void SetRecipe(CraftingRecipe recipe)
    {
        if (recipe == null)
        {
            resultImage.sprite = null;
            resultImage.enabled = false;
           // resultName.text = "";
          //  ingredientsText.text = "";
            return;
        }
        resultImage.enabled = true;
        resultImage.sprite = recipe.result.itemData.itemIcon;
       // resultName.text = recipe.result.itemData.itemName;
        string ingredientsStr = "";
        for (int i = 0; i < recipe.ingredients.Length; i++)
        {
            var ingredient = recipe.ingredients[i];
            var count = recipe.ingredientCounts[i];
            ingredientsStr += $"{ingredient.itemData.itemName} x{count}\n";
        }
       // ingredientsText.text = ingredientsStr.TrimEnd('\n');
    }
}