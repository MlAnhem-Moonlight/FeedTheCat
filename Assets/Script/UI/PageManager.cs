using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class PageManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HorizontalScrollSnap scrollSnap;
    [SerializeField] private GameObject pagePrefab;

    [Header("Level")]
    [SerializeField] private List<LevelData> allLevels;
    [SerializeField] private int levelsPerPage = 6;

    private readonly List<GameObject> pages = new();
    private bool pagesBuilt = false; // guard to prevent duplicate builds
    private int lastBuiltLevelCount = -1; // remember how many levels were used for the last build

    private void Awake()
    {
        if (scrollSnap == null)
            scrollSnap = GetComponent<HorizontalScrollSnap>();
    }

    private IEnumerator Start()
    {
        yield return null;

        // Only build pages at Start if level data is already assigned.
        // When GameManager sets levels on scene load it will call SetLevels and build pages,
        // so avoid rebuilding here which causes duplicate initialization.
        if (allLevels != null && allLevels.Count > 0)
        {
            BuildPages();
        }
    }
    public void SetLevels(List<LevelData> levels)
    {
        allLevels = new List<LevelData>(levels);
        BuildPages();
    }

    public void Refresh()
    {
        BuildPages();
    }

    //private void BuildPages()
    //{
    //    if (scrollSnap == null)
    //    {
    //        Debug.LogError("HorizontalScrollSnap missing.");
    //        return;
    //    }

    //    //---------------------------------
    //    // Remove old pages
    //    //---------------------------------

    //    scrollSnap.RemoveAllChildren(out GameObject[] removed);

    //    foreach (GameObject go in removed)
    //    {
    //        Destroy(go);
    //    }

    //    pages.Clear();

    //    //---------------------------------
    //    // Create pages
    //    //---------------------------------

    //    int totalPages =
    //        Mathf.CeilToInt((float)allLevels.Count / levelsPerPage);

    //    for (int page = 0; page < totalPages; page++)
    //    {
    //        GameObject pageObj = Instantiate(pagePrefab);

    //        pageObj.name = $"Page {page + 1}";

    //        LevelButtonManager manager =
    //            pageObj.GetComponent<LevelButtonManager>();

    //        if (manager == null)
    //            manager = pageObj.GetComponentInChildren<LevelButtonManager>();

    //        List<LevelData> pageLevels = new();

    //        int start = page * levelsPerPage;
    //        int end = Mathf.Min(start + levelsPerPage, allLevels.Count);

    //        for (int i = start; i < end; i++)
    //            pageLevels.Add(allLevels[i]);

    //        manager.SpawnLevelButtons(pageLevels);

    //        scrollSnap.AddChild(pageObj);

    //        pages.Add(pageObj);
    //    }

    //    //---------------------------------
    //    // Refresh layout
    //    //---------------------------------

    //    Canvas.ForceUpdateCanvases();

    //    scrollSnap.UpdateLayout(true);

    //    scrollSnap.GoToScreen(0);
    //}

    private void BuildPages()
    {
        if (scrollSnap == null)
        {
            Debug.LogError("HorizontalScrollSnap is null!");
            return;
        }

        // Avoid rebuilding if we already built for the same level count
        if (pagesBuilt && allLevels != null && allLevels.Count == lastBuiltLevelCount)
        {
            // suppressed debug message by global log filter
            return;
        }

        ScrollRect scrollRect = scrollSnap.GetComponent<ScrollRect>();

        if (scrollRect == null)
        {
            Debug.LogError("ScrollRect not found!");
            return;
        }

        Transform content = scrollRect.content;

        // Clear existing pages in content
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        pages.Clear();

        // Also clear pagination toggles (if the scroll snap has a pagination container)
        ScrollSnapBase ssBase = scrollSnap as ScrollSnapBase;
        if (ssBase != null && ssBase.Pagination != null)
        {
            for (int i = ssBase.Pagination.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(ssBase.Pagination.transform.GetChild(i).gameObject);
            }
        }

        Canvas.ForceUpdateCanvases();

        if (allLevels == null || allLevels.Count == 0)
        {
            // Still call Rebuild so the scroll snap updates its state, but remember we built 0 levels
            lastBuiltLevelCount = allLevels == null ? 0 : allLevels.Count;
            pagesBuilt = true;
            scrollSnap.Rebuild();
            return;
        }

        int totalPages = Mathf.CeilToInt((float)allLevels.Count / levelsPerPage);

        for (int page = 0; page < totalPages; page++)
        {
            GameObject pageObj = Instantiate(pagePrefab, content, false);
            pageObj.name = $"Page_{page + 1}";

            LevelButtonManager manager = pageObj.GetComponent<LevelButtonManager>();

            if (manager == null)
                manager = pageObj.GetComponentInChildren<LevelButtonManager>();

            if (manager == null)
            {
                Debug.LogError($"Page {page + 1} doesn't contain LevelButtonManager.");
                continue;
            }

            List<LevelData> pageLevels = new();

            int start = page * levelsPerPage;
            int end = Mathf.Min(start + levelsPerPage, allLevels.Count);

            for (int i = start; i < end; i++)
            {
                pageLevels.Add(allLevels[i]);
            }

            manager.SpawnLevelButtons(pageLevels);

            pages.Add(pageObj);
        }

        Canvas.ForceUpdateCanvases();

        // mark build info before triggering UI callbacks to avoid re-entrancy issues
        lastBuiltLevelCount = allLevels.Count;
        pagesBuilt = true;

        scrollSnap.Rebuild();

        scrollSnap.GoToScreen(0);
    }

    public void RefreshButtons()
    {
        foreach (GameObject page in pages)
        {
            LevelButtonManager manager =
                page.GetComponent<LevelButtonManager>();

            if (manager != null)
                manager.RefreshButtons();
        }
    }

    public void AddLevel(LevelData level)
    {
        allLevels.Add(level);
        BuildPages();
    }
}