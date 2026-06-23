using UnityEngine;

// Utility to check RectTransform overlaps for UI elements (debug collisions)
public static class CollisionDebug
{
    public static bool RectsOverlap(RectTransform a, RectTransform b)
    {
        if (a == null || b == null) return false;

        Vector3[] ac = new Vector3[4];
        Vector3[] bc = new Vector3[4];
        a.GetWorldCorners(ac);
        b.GetWorldCorners(bc);

        float aMinX = Mathf.Min(ac[0].x, ac[1].x, ac[2].x, ac[3].x);
        float aMaxX = Mathf.Max(ac[0].x, ac[1].x, ac[2].x, ac[3].x);
        float aMinY = Mathf.Min(ac[0].y, ac[1].y, ac[2].y, ac[3].y);
        float aMaxY = Mathf.Max(ac[0].y, ac[1].y, ac[2].y, ac[3].y);

        float bMinX = Mathf.Min(bc[0].x, bc[1].x, bc[2].x, bc[3].x);
        float bMaxX = Mathf.Max(bc[0].x, bc[1].x, bc[2].x, bc[3].x);
        float bMinY = Mathf.Min(bc[0].y, bc[1].y, bc[2].y, bc[3].y);
        float bMaxY = Mathf.Max(bc[0].y, bc[1].y, bc[2].y, bc[3].y);

        bool overlap = (aMinX <= bMaxX && aMaxX >= bMinX) && (aMinY <= bMaxY && aMaxY >= bMinY);
        return overlap;
    }
}
