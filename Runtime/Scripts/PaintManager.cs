using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PaintManager : MonoBehaviour
{
    // ====== Drawing Prefab / Camera ======
    public GameObject baseRenderer;        // Prefab with TrailRenderer (the brush)
    public TrailRenderer defaultBrush;     // Optional: used by setters for future strokes
    public Camera mainCamera;              // World camera used to convert screen → world
    public float zDistance;                // World Z depth where drawing happens
    public float smoothness;

    // ====== UI Area Restriction ======
    public Canvas targetCanvas;                  // Canvas that holds the draw area
    public RectTransform drawArea;               // ONLY this UI rect allows drawing
    public bool requireGraphicHit = false;       // Respect alpha/raycastTarget when true

    [Header("Inner margin (pixels)")]
    [Tooltip("Shrink the allowed region inward from each edge by this many pixels. If the cursor is within this distance from any edge, drawing is blocked.")]
    public float innerMarginPx = 0f;

    // ====== Stroke draw order (new strokes on top) ======
    [Tooltip("Next sorting order to use for new strokes (TrailRenderer.sortingOrder).")]
    [SerializeField] private int nextSortingOrder = 0;

    [Tooltip("If true, read initial sortingOrder from the brush prefab on Start().")]
    [SerializeField] private bool initSortingFromBrush = true;

    // Internals
    GameObject currentTrail;

    // Optional event
    public event Action OnGameFinish;

    private void Start()
    {
        if (baseRenderer == null)
            Debug.LogError("DrawManager: baseRenderer is not assigned.");
        if (mainCamera == null)
            Debug.LogError("DrawManager: mainCamera is not assigned.");
        if (targetCanvas == null)
            Debug.LogWarning("DrawManager: targetCanvas is not assigned. Area checks may fail.");
        if (drawArea == null)
            Debug.LogWarning("DrawManager: drawArea is not assigned. Drawing will not be restricted.");
        if (EventSystem.current == null && requireGraphicHit)
            Debug.LogWarning("DrawManager: No EventSystem found. UI raycasts will not work.");

        if (initSortingFromBrush && baseRenderer != null)
        {
            var prefabTrail = baseRenderer.GetComponent<TrailRenderer>()
                             ?? baseRenderer.GetComponentInChildren<TrailRenderer>(true);
            if (prefabTrail != null)
                nextSortingOrder = prefabTrail.sortingOrder;
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        // Prefer touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(Input.touchCount - 1);
            if (!IsTouchOverDrawAreaWithMargin(touch.position)) return;

            Vector3 touchPos = mainCamera.ScreenToWorldPoint(
                new Vector3(touch.position.x, touch.position.y, zDistance));

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    BeginStroke(touchPos);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    MoveStroke(touchPos);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    EndStroke(touchPos);
                    break;
            }
            return;
        }

        // Mouse (Editor/Standalone) – optional, remove if you don’t need it
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
        {
            if (!IsTouchOverDrawAreaWithMargin(Input.mousePosition)) return;

            var phase =
                Input.GetMouseButtonDown(0) ? TouchPhase.Began :
                Input.GetMouseButton(0) ? TouchPhase.Moved :
                                              TouchPhase.Ended;

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance));

            if (phase == TouchPhase.Began) BeginStroke(worldPos);
            else if (phase == TouchPhase.Moved) MoveStroke(worldPos);
            else EndStroke(worldPos);
        }
    }

    // =========================
    // Stroke lifecycle
    // =========================
    private void BeginStroke(Vector3 worldPos)
    {
        if (baseRenderer == null) return;

        currentTrail = Instantiate(baseRenderer.gameObject, worldPos, transform.rotation, transform);
        if (currentTrail == null) return;

        BumpTrailSortingOrder(currentTrail);

        currentTrail.transform.position = Vector3.Lerp(
            currentTrail.transform.position, worldPos, smoothness * Time.deltaTime);
    }

    private void MoveStroke(Vector3 worldPos)
    {
        if (currentTrail == null) return;
        currentTrail.transform.position = Vector3.Lerp(
            currentTrail.transform.position, worldPos, smoothness * Time.deltaTime);
    }

    private void EndStroke(Vector3 worldPos)
    {
        if (currentTrail != null)
        {
            currentTrail.transform.position = worldPos;
            currentTrail = null;
            // OnGameFinish?.Invoke();
        }
    }

    // =========================
    // AREA CHECK WITH MARGIN
    // =========================
    private bool IsTouchOverDrawAreaWithMargin(Vector2 screenPos)
    {
        if (drawArea == null || targetCanvas == null) return false;

        Camera uiCam = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : targetCanvas.worldCamera;

        // 1) If we require an actual UI hit (respects alpha & raycastTarget), check that first
        if (requireGraphicHit && EventSystem.current != null)
        {
            var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            bool hitDrawAreaHierarchy = false;
            for (int i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                if (go == drawArea.gameObject || go.transform.IsChildOf(drawArea))
                {
                    hitDrawAreaHierarchy = true;
                    break;
                }
            }
            if (!hitDrawAreaHierarchy) return false; // didn’t hit the graphic at all
        }

        // 2) Now apply the inner margin check (inset rectangle)
        return RectangleContainsScreenPointWithMargin(drawArea, screenPos, uiCam, innerMarginPx);
    }

    /// <summary>
    /// Like RectTransformUtility.RectangleContainsScreenPoint, but shrinks the rect by `marginPx`
    /// from every edge before testing. If the margin is too large (rect collapses), returns false.
    /// </summary>
    private static bool RectangleContainsScreenPointWithMargin(
        RectTransform rect, Vector2 screenPos, Camera cam, float marginPx)
    {
        // Convert to rect-local coordinates
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, cam, out var local))
            return false;

        // Get the unscaled local rect
        Rect r = rect.rect;

        // Shrink from each edge by marginPx
        float left = r.xMin + marginPx;
        float right = r.xMax - marginPx;
        float bottom = r.yMin + marginPx;
        float top = r.yMax - marginPx;

        if (right <= left || top <= bottom)
            return false; // margin too large: no drawable area

        return (local.x >= left && local.x <= right && local.y >= bottom && local.y <= top);
    }

    // =========================
    // BRUSH HELPERS
    // =========================
    public void SetBrush(GameObject brush) => baseRenderer = brush;

    public void SetBrushSize(float multiplier = 1)
    {
        if (defaultBrush != null)
            defaultBrush.widthMultiplier = multiplier;
    }

    public void SetPattern(Texture2D tex)
    {
        if (defaultBrush == null || tex == null) return;

        Material newMat = new Material(defaultBrush.sharedMaterial);
        if (newMat.HasProperty("_BaseMap")) newMat.SetTexture("_BaseMap", tex);
        else newMat.SetTexture("_MainTex", tex);
        defaultBrush.material = newMat;
    }

    private void BumpTrailSortingOrder(GameObject strokeGO)
    {
        if (strokeGO == null) return;
        var tr = strokeGO.GetComponent<TrailRenderer>() ?? strokeGO.GetComponentInChildren<TrailRenderer>(true);
        if (tr != null)
        {
            nextSortingOrder += 1;
            tr.sortingOrder = nextSortingOrder;
        }
    }

    // =========================
    // EDITING HELPERS
    // =========================
    public void Undo()
    {
        int count = transform.childCount;
        if (count == 0) return;
        Transform last = transform.GetChild(count - 1);
        Destroy(last.gameObject);
    }

    public void Wipe()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            Destroy(child.gameObject);
        }
    }
}
