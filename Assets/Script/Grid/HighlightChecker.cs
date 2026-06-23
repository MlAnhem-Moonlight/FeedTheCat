using UnityEngine;

public static class HighlightChecker
{
    // Return true if it's appropriate to override the cell's sprite when highlighting.
    // We avoid changing the sprite for destination cells so their dedicated visuals remain intact.
    public static bool ShouldApplyHighlightSprite(GridCell cell)
    {
        if (cell == null) return false;
        return !cell.isDestination;
    }
}
