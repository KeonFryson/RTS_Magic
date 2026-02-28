using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CarouselUI : MonoBehaviour
{
    public GameObject slotPrefab;
    public Transform carouselPanel;
    public Button previousButton;
    public Button nextButton;
    public Button CraftButton;

    private int currentIndex = 0;
    private readonly List<GameObject> currentSlots = new();

    void Start()
    {
        UpdateCarousel();
        previousButton.onClick.AddListener(ShowPrevious);
        nextButton.onClick.AddListener(ShowNext);
        CraftButton.onClick.AddListener(OnCraftButtonClicked);
    }

    void UpdateCarousel()
    {
        // Destroy all children of carouselPanel (not just those in currentSlots)
        for (int i = carouselPanel.childCount - 1; i >= 0; i--)
        {
            Destroy(carouselPanel.GetChild(i).gameObject);
        }
        currentSlots.Clear();

        var recipes = CraftingManager.Instance != null ? CraftingManager.Instance.recipes : null;
        if (recipes == null || recipes.Count == 0)
            return;

        int prevIndex = (currentIndex - 1 + recipes.Count) % recipes.Count;
        int nextIndex = (currentIndex + 1) % recipes.Count;

        var prevSlot = Instantiate(slotPrefab, carouselPanel);
        prevSlot.GetComponent<CarouselSlot>().SetRecipe(recipes[prevIndex]);
        currentSlots.Add(prevSlot);

        var currSlot = Instantiate(slotPrefab, carouselPanel);
        var currSlotComp = currSlot.GetComponent<CarouselSlot>();
        currSlotComp.SetRecipe(recipes[currentIndex]);
        currentSlots.Add(currSlot);

        if (currSlotComp.resultOutline != null)
            currSlotComp.resultOutline.enabled = true;

        var nextSlot = Instantiate(slotPrefab, carouselPanel);
        nextSlot.GetComponent<CarouselSlot>().SetRecipe(recipes[nextIndex]);
        currentSlots.Add(nextSlot);

        prevSlot.transform.SetSiblingIndex(0);
        currSlot.transform.SetSiblingIndex(1);
        nextSlot.transform.SetSiblingIndex(2);
    }

    void ShowPrevious()
    {
        var recipes = CraftingManager.Instance != null ? CraftingManager.Instance.recipes : null;
        if (recipes == null || recipes.Count == 0) return;
        currentIndex = (currentIndex - 1 + recipes.Count) % recipes.Count;
        UpdateCarousel();
    }

    void ShowNext()
    {
        var recipes = CraftingManager.Instance != null ? CraftingManager.Instance.recipes : null;
        if (recipes == null || recipes.Count == 0) return;
        currentIndex = (currentIndex + 1) % recipes.Count;
        UpdateCarousel();
    }

    public CraftingRecipe GetCurrentRecipe()
    {
        var recipes = CraftingManager.Instance != null ? CraftingManager.Instance.recipes : null;
        if (recipes == null || recipes.Count == 0) return null;
        return recipes[currentIndex];
    }

    public void SetCraftButtonInteractable(bool interactable)
    {
        CraftButton.interactable = interactable;
    }

    public void OnCraftButtonClicked()
    {
        var recipe = GetCurrentRecipe();
        if (recipe != null)
        {
            CraftingManager.Instance.Craft(recipe, Inventory.Instance);
            Debug.Log($"Crafting {recipe.result.name}");
        }
    }
}