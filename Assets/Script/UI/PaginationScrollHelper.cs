using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper class to manage pagination with ScrollRect when there are many toggles
/// </summary>
public class PaginationScrollHelper : MonoBehaviour
{
    private ScrollRect paginationScrollRect;
    private VerticalLayoutGroup layoutGroup;
    private const int MaxTogglesToFitWithoutScroll = 15;

    /// <summary>
    /// Setup pagination scroll if needed (when toggle count exceeds threshold)
    /// </summary>
    public void SetupPaginationScroll(Transform paginationContainer, int toggleCount)
    {
        if (paginationContainer == null)
            return;

        // Only add scroll if we have too many toggles
        if (toggleCount <= MaxTogglesToFitWithoutScroll)
        {
            // Remove scroll if it exists but not needed
            if (paginationScrollRect != null)
            {
                Destroy(paginationScrollRect.gameObject);
                paginationScrollRect = null;
            }
            return;
        }

        // Check if pagination container already has a ScrollRect
        paginationScrollRect = paginationContainer.GetComponent<ScrollRect>();

        if (paginationScrollRect == null)
        {
            // Create new ScrollRect for pagination
            paginationScrollRect = paginationContainer.gameObject.AddComponent<ScrollRect>();

            // Configure ScrollRect
            paginationScrollRect.horizontal = false;  // Vertical only
            paginationScrollRect.vertical = true;
            paginationScrollRect.horizontalScrollbar = null;
            paginationScrollRect.verticalScrollbar = null;
            paginationScrollRect.scrollSensitivity = 5f;
            paginationScrollRect.inertia = true;
            paginationScrollRect.decelerationRate = 0.95f;

            // Get or add VerticalLayoutGroup
            layoutGroup = paginationContainer.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = paginationContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            // Configure VerticalLayoutGroup for toggle spacing
            layoutGroup.spacing = 5f;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childScaleHeight = false;
            layoutGroup.childScaleWidth = false;

            // Get or add RectTransform with proper setup
            RectTransform paginationRect = paginationContainer.GetComponent<RectTransform>();
            if (paginationRect != null)
            {
                // Preserve existing size
                paginationRect.anchorMin = new Vector2(0.5f, 0f);
                paginationRect.anchorMax = new Vector2(0.5f, 0f);
                paginationRect.pivot = new Vector2(0.5f, 0f);
            }

            // Set content for scroll
            // In HorizontalScrollSnap, pagination container IS the content, so no separate content needed
            paginationScrollRect.content = paginationRect;

            Debug.Log($"PaginationScrollHelper: Added ScrollRect to pagination container for {toggleCount} toggles");
        }

        // Ensure layout group is configured
        if (layoutGroup == null)
        {
            layoutGroup = paginationContainer.GetComponent<VerticalLayoutGroup>();
        }

        // Force layout rebuild
        LayoutRebuilder.ForceRebuildLayoutImmediate(paginationContainer as RectTransform);
    }

    /// <summary>
    /// Scroll pagination to show a specific page toggle
    /// </summary>
    public void ScrollPaginationToToggle(Transform paginationContainer, int toggleIndex)
    {
        if (paginationScrollRect == null || paginationContainer == null)
            return;

        // Get target toggle
        if (toggleIndex < 0 || toggleIndex >= paginationContainer.childCount)
            return;

        Transform targetToggle = paginationContainer.GetChild(toggleIndex);
        if (targetToggle == null)
            return;

        RectTransform toggleRect = targetToggle.GetComponent<RectTransform>();
        RectTransform contentRect = paginationScrollRect.content;

        if (toggleRect == null || contentRect == null)
            return;

        // Calculate scroll position to center the toggle
        float toggleHeight = toggleRect.rect.height;
        float contentHeight = contentRect.rect.height;
        float scrollRectHeight = (paginationScrollRect.transform as RectTransform).rect.height;

        // Position of toggle relative to content
        float toggleY = -toggleRect.anchoredPosition.y;
        float targetY = toggleY - (scrollRectHeight - toggleHeight) / 2f;

        // Clamp to valid range
        targetY = Mathf.Clamp(targetY, 0, contentHeight - scrollRectHeight);

        // Set scroll position
        paginationScrollRect.verticalNormalizedPosition = 1f - (targetY / (contentHeight - scrollRectHeight));

        Debug.Log($"PaginationScrollHelper: Scrolled pagination to toggle {toggleIndex}");
    }

    /// <summary>
    /// Clear pagination scroll (called when rebuilding)
    /// </summary>
    public void ClearPaginationScroll()
    {
        if (paginationScrollRect != null)
        {
            Destroy(paginationScrollRect);
            paginationScrollRect = null;
        }
    }
}
