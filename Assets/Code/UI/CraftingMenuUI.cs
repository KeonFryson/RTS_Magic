using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class CraftingMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject recipeSlotPrefab;
    [SerializeField] private Transform gridPanel; // Should be a GridLayoutGroup
    [SerializeField] private ScrollRect scrollRect;

    [Header("Tab Buttons")]
    [SerializeField] private Button partsTabButton;
    [SerializeField] private Button buildingsTabButton;
    [SerializeField] private Button weaponsTabButton;

    private readonly List<GameObject> recipeSlots = new();
    private RecipeCategory currentCategory = RecipeCategory.Parts;

    private Color normalTabColor = Color.white;
    private Color disabledTabColor = Color.gray;

    private void Awake()
    {
        if (partsTabButton != null)
            partsTabButton.onClick.AddListener(() => OnTabSelected(RecipeCategory.Parts));
        if (buildingsTabButton != null)
            buildingsTabButton.onClick.AddListener(() => OnTabSelected(RecipeCategory.Buildings));
        if (weaponsTabButton != null)
            weaponsTabButton.onClick.AddListener(() => OnTabSelected(RecipeCategory.Weapons));
    }

    private void Start()
    {
        UpdateTabButtons();
        RefreshGrid();
    }

    private void OnEnable()
    {
        UpdateTabButtons();
        RefreshGrid();
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += RefreshGrid;
    }

    private void OnDisable()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= RefreshGrid;
    }

    private void OnTabSelected(RecipeCategory category)
    {
        currentCategory = category;
        UpdateTabButtons();
        RefreshGrid();
    }

    private void UpdateTabButtons()
    {
        SetTabButtonState(partsTabButton, currentCategory == RecipeCategory.Parts);
        SetTabButtonState(buildingsTabButton, currentCategory == RecipeCategory.Buildings);
        SetTabButtonState(weaponsTabButton, currentCategory == RecipeCategory.Weapons);
    }

    private void SetTabButtonState(Button button, bool isActive)
    {
        if (button == null)
            return;

        button.interactable = !isActive;
        var colors = button.colors;
        colors.normalColor = isActive ? disabledTabColor : normalTabColor;
        colors.disabledColor = disabledTabColor;
        button.colors = colors;
    }

    public void RefreshGrid()
    {
        var recipes = CraftingManager.Instance != null ? CraftingManager.Instance.recipes : null;
        if (recipes == null || recipes.Count == 0)
            return;

        // Filter recipes by current category
        var filteredRecipes = recipes.Where(r => r.category == currentCategory).ToList();

        // Create or destroy slots to match the number of recipes
        while (recipeSlots.Count < filteredRecipes.Count)
        {
            var slotObj = Instantiate(recipeSlotPrefab, gridPanel);
            var slotUI = slotObj.GetComponent<RecipeSlotUI>();
            slotUI.button.onClick.AddListener(() => OnRecipeClicked(slotUI.CurrentRecipe));
            recipeSlots.Add(slotObj);
        }
        while (recipeSlots.Count > filteredRecipes.Count)
        {
            Destroy(recipeSlots[recipeSlots.Count - 1]);
            recipeSlots.RemoveAt(recipeSlots.Count - 1);
        }

        // Update all slots
        for (int i = 0; i < filteredRecipes.Count; i++)
        {
            var recipe = filteredRecipes[i];
            var slotObj = recipeSlots[i];
            var slotUI = slotObj.GetComponent<RecipeSlotUI>();
            slotUI.SetRecipe(recipe);

            bool canCraft = CraftingManager.Instance.CanCraft(recipe, Inventory.Instance);
            slotUI.SetInteractable(canCraft);

            // Store the recipe reference for the click event
            slotUI.CurrentRecipe = recipe;

            // Ensure the slot is active
            slotObj.SetActive(true);
        }
    }

    private void OnRecipeClicked(CraftingRecipe recipe)
    {
        if (CraftingManager.Instance.CanCraft(recipe, Inventory.Instance))
        {
            CraftingManager.Instance.Craft(recipe, Inventory.Instance);
            RefreshGrid();
            Debug.Log($"Crafted {recipe.result.name}");
        }
    }
}