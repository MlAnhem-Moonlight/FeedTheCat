using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UICardSplineGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private SplineContainer splineContainer;

    [Tooltip("Root chứa các SplineAnimate. Để trống sẽ tự tìm trong object này.")]
    [SerializeField] private Transform vfxRoot;

    private Vector2 _lastSize;

    //private void Reset()
    //{
    //    targetRect = GetComponent<RectTransform>();

    //    if (splineContainer == null)
    //        splineContainer = GetComponent<SplineContainer>();
    //}

    private void Awake()
    {
        GenerateSpline();
    }

    private void OnEnable()
    {
        GenerateSpline();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (targetRect.rect.size != _lastSize)
                GenerateSpline();
        }
#endif
    }

    private void OnRectTransformDimensionsChange()
    {
        GenerateSpline();
    }

    public void GenerateSpline()
    {
        if (targetRect == null || splineContainer == null)
        {
            Debug.Log("UICardSplineGenerator: Missing references. Attempting to auto-assign.");
            targetRect = GameObject.Find("SplineCard")?.GetComponent<RectTransform>();
            splineContainer = GetComponent<SplineContainer>();
            //return;
        }


        Rect rect = targetRect.rect;

        _lastSize = rect.size;

        float left = rect.xMin;
        float right = rect.xMax;
        float top = rect.yMax;
        float bottom = rect.yMin;

        var spline = splineContainer.Spline;
        spline.Clear();

        // Chiều kim đồng hồ:
        // TopLeft -> TopRight -> BottomRight -> BottomLeft
        spline.Add(new BezierKnot(new float3(left, top, 0)));
        spline.Add(new BezierKnot(new float3(right, top, 0)));
        spline.Add(new BezierKnot(new float3(right, bottom, 0)));
        spline.Add(new BezierKnot(new float3(left, bottom, 0)));

        spline.Closed = true;

        AssignSplineToAnimators();
    }

    private void AssignSplineToAnimators()
    {
        Transform root = vfxRoot == null ? transform : vfxRoot;

        var animators = root.GetComponentsInChildren<SplineAnimate>(true);

        foreach (var animator in animators)
        {
            animator.Container = splineContainer;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(animator);
#endif
        }
    }
}