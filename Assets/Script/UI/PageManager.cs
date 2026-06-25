using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using System.Collections.Generic;

/// <summary>
/// Manages level pages and pagination.
/// Attach this to the HorizontalScrollSnap container.
/// Handles creating pages, distributing levels, and managing pagination dots.
/// </summary>
public class PageManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HorizontalScrollSnap horizontalScrollSnap;
    [SerializeField] private GameObject pagePrefab;
    [SerializeField] private Transform paginationContainer;
    [SerializeField] private Toggle paginationDotPrefab;

    [Header("Level Data")]
    [SerializeField] private List<LevelData> allLevels = new List<LevelData>();
    [SerializeField] private int maxLevelsPerPage = 6;
    [SerializeField] private int maxPages = 4;

    [Header("Configuration")]
    [SerializeField] private LevelButtonManager levelButtonManagerPrefab;

    private List<GameObject> pages = new List<GameObject>();
    private List<Toggle> paginationDots = new List<Toggle>();
    private int currentPageIndex = 0;

    private void Start()
    {
        if (horizontalScrollSnap == null)
            horizontalScrollSnap = GetComponent<HorizontalScrollSnap>();

        if (horizontalScrollSnap == null)
        {
            Debug.LogError("PageManager: HorizontalScrollSnap component not found!");
            return;
        }

        InitializePages();
    }

    /// <summary>
    /// Initialize pages with level data
    /// </summary>
    public void InitializePages()
    {
        if (allLevels == null || allLevels.Count == 0)
        {
            Debug.LogWarning("PageManager: No level data provided!");
            return;
        }

        Debug.Log($"PageManager: Initializing with {allLevels.Count} levels, {maxLevelsPerPage} per page");

        ClearPages();

        int totalPages = Mathf.CeilToInt((float)allLevels.Count / maxLevelsPerPage);
        totalPages = Mathf.Min(totalPages, maxPages);

        Debug.Log($"PageManager: Calculated {totalPages} pages (limited by maxPages={maxPages})");

        for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
        {
            CreatePage(pageIndex);
        }

        CreatePaginationDots(totalPages);
        Debug.Log($"PageManager: Created {totalPages} pagination dots");

        horizontalScrollSnap.UpdateLayout(true);
    }

    private void CreatePage(int pageIndex)
    {
        if (pagePrefab == null)
        {
            Debug.LogError("PageManager: pagePrefab is not assigned!");
            return;
        }

        GameObject pageGo = Instantiate(pagePrefab, horizontalScrollSnap.transform, false);
        pageGo.name = $"Page_{pageIndex}";

        LevelButtonManager buttonManager = pageGo.GetComponent<LevelButtonManager>();
        if (buttonManager == null)
        {
            // Try to find it in children
            buttonManager = pageGo.GetComponentInChildren<LevelButtonManager>();
        }

        if (buttonManager == null)
        {
            // Create one if it doesn't exist
            buttonManager = pageGo.AddComponent<LevelButtonManager>();
        }

        // Calculate which levels go on this page
        int startIndex = pageIndex * maxLevelsPerPage;
        int endIndex = Mathf.Min(startIndex + maxLevelsPerPage, allLevels.Count);

        List<LevelData> pageLevels = new List<LevelData>();
        for (int i = startIndex; i < endIndex; i++)
        {
            pageLevels.Add(allLevels[i]);
        }

        buttonManager.SpawnLevelButtons(pageLevels);
        pages.Add(pageGo);

        Debug.Log($"PageManager: Created page {pageIndex} with {pageLevels.Count} levels");
    }

    private void CreatePaginationDots(int pageCount)
    {
        if (paginationContainer == null)
        {
            Debug.LogWarning("PageManager: paginationContainer is not assigned. Skipping pagination dots.");
            return;
        }

        if (paginationDotPrefab == null)
        {
            Debug.LogWarning("PageManager: paginationDotPrefab is not assigned. Skipping pagination dots.");
            return;
        }

        // Clear existing dots
        foreach (Transform child in paginationContainer)
        {
            Destroy(child.gameObject);
        }
        paginationDots.Clear();

        // Create new dots
        for (int i = 0; i < pageCount; i++)
        {
            Toggle dotGo = Instantiate(paginationDotPrefab, paginationContainer, false);
            dotGo.name = $"Dot_{i}";

            int pageIndex = i; // Capture for closure
            dotGo.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    horizontalScrollSnap.GoToScreen(pageIndex);
                }
            });

            paginationDots.Add(dotGo);
        }

        // Set first dot as active
        if (paginationDots.Count > 0)
        {
            paginationDots[0].isOn = true;
        }

        Debug.Log($"PageManager: Created {pageCount} pagination dots");
    }

    /// <summary>
    /// Update pagination when page changes (call from HorizontalScrollSnap if needed)
    /// </summary>
    public void OnPageChanged(int newPageIndex)
    {
        currentPageIndex = newPageIndex;

        // Update dot visibility
        for (int i = 0; i < paginationDots.Count; i++)
        {
            paginationDots[i].isOn = (i == newPageIndex);
        }

        Debug.Log($"PageManager: Switched to page {newPageIndex}");
    }

    /// <summary>
    /// Add a new level to the manager and create pages if needed
    /// </summary>
    public void AddLevel(LevelData levelData)
    {
        if (levelData == null) return;

        allLevels.Add(levelData);

        // Rebuild pages if necessary
        int neededPages = Mathf.CeilToInt((float)allLevels.Count / maxLevelsPerPage);
        if (neededPages > pages.Count)
        {
            CreatePage(pages.Count);
            CreatePaginationDots(neededPages);
            horizontalScrollSnap.UpdateLayout();
        }
    }

    /// <summary>
    /// Set all levels at once
    /// </summary>
    public void SetLevels(List<LevelData> levels)
    {
        if (levels == null) return;
        allLevels = new List<LevelData>(levels);
        InitializePages();
    }

    /// <summary>
    /// Refresh all buttons (useful after unlocking levels)
    /// </summary>
    public void RefreshAllPages()
    {
        foreach (var page in pages)
        {
            LevelButtonManager manager = page.GetComponent<LevelButtonManager>();
            if (manager != null)
            {
                manager.RefreshButtons();
            }
        }
    }

    private void ClearPages()
    {
        foreach (var page in pages)
        {
            if (page != null)
                Destroy(page);
        }
        pages.Clear();

        foreach (var dot in paginationDots)
        {
            if (dot != null)
                Destroy(dot.gameObject);
        }
        paginationDots.Clear();
    }

    private void OnDestroy()
    {
        ClearPages();
    }
}
