using System;
using UnityEngine;

public class NeonManager : MonoBehaviour
{
    public GameObject baseRenderer;
    [ColorUsageAttribute(true, true)]
    public Color[] trailColors;

    public Camera mainCamera;
    public float zDistance;

    int nextColor = 0;
    GameObject currentTrail;
    public float smoothness;

    MaterialPropertyBlock trailBlock;

    public event Action OnGameFinish;

    private void Start()
    {
        trailBlock = new MaterialPropertyBlock();

        if (baseRenderer == null)
        {
            Debug.LogError("NeonManager: baseRenderer is not assigned.");
        }
        if (mainCamera == null)
        {
            Debug.LogError("NeonManager: mainCamera is not assigned.");
        }
        if (trailColors == null || trailColors.Length == 0)
        {
            Debug.LogError("NeonManager: trailColors array is empty or not assigned.");
        }
    }

    void Update()
    {
        if (Input.touchCount == 0 || mainCamera == null)
        {
            return; // Exit early if no touch input or missing camera.
        }

        Touch touch = Input.GetTouch(Input.touchCount - 1);
        Vector3 touchPos = mainCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, zDistance));

        switch (touch.phase)
        {
            case TouchPhase.Began:
                if (baseRenderer == null)
                {
                    Debug.LogWarning("NeonManager: baseRenderer is null during TouchPhase.Began.");
                    return;
                }

                currentTrail = Instantiate(baseRenderer.gameObject, touchPos, transform.rotation, transform);

                if (currentTrail == null)
                {
                    Debug.LogWarning("NeonManager: Failed to instantiate currentTrail.");
                    return;
                }

                TrailRenderer currentRenderer = currentTrail.GetComponent<TrailRenderer>();
                if (currentRenderer == null)
                {
                    Debug.LogError("NeonManager: TrailRenderer component is missing on baseRenderer.");
                    return;
                }

                if (trailColors != null && trailColors.Length > 0 && trailBlock != null)
                {
                    Color colorToUse = trailColors[nextColor];
                    trailBlock.SetColor("_HDR", colorToUse);
                    nextColor = (nextColor + 1) % trailColors.Length;
                    currentRenderer.SetPropertyBlock(trailBlock);
                }
                else
                {
                    Debug.LogWarning("NeonManager: trailColors or trailBlock is not properly set.");
                }

                if (currentTrail != null)
                {
                    currentTrail.transform.position = Vector3.Lerp(currentTrail.transform.position, touchPos, smoothness * Time.deltaTime);
                }
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (currentTrail != null)
                {
                    currentTrail.transform.position = Vector3.Lerp(currentTrail.transform.position, touchPos, smoothness * Time.deltaTime);
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (currentTrail != null)
                {
                    currentTrail.transform.position = touchPos;
                    currentTrail = null;
                }
                break;
        }
    }
}
