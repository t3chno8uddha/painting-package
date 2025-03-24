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
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount == 0) return; // Exit early if no touch input.

        Touch touch = Input.GetTouch(Input.touchCount - 1);  // Get the first touch input.
        Vector3 touchPos = mainCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, zDistance));

        switch (touch.phase)
        {
            case TouchPhase.Began: // Raycast to check if the touch is on this object.
                currentTrail = Instantiate(baseRenderer.gameObject, touchPos, transform.rotation, transform);

                TrailRenderer currentRenderer = currentTrail.GetComponent<TrailRenderer>();

                trailBlock.SetColor("_HDR", trailColors[nextColor]);
                nextColor++;

                if (nextColor == trailColors.Length) nextColor = 0;

                currentRenderer.SetPropertyBlock(trailBlock);

                currentTrail.transform.position = Vector3.Lerp(currentTrail.transform.position, touchPos, smoothness * Time.deltaTime);

                break;

            case TouchPhase.Moved:  // Update position only if it has changed.
                currentTrail.transform.position = Vector3.Lerp(currentTrail.transform.position, touchPos, smoothness * Time.deltaTime);
                break;
            case TouchPhase.Stationary:
                currentTrail.transform.position = Vector3.Lerp(currentTrail.transform.position, touchPos, smoothness * Time.deltaTime);
                break;
            case TouchPhase.Ended:  // Release the object when touch ends.
                currentTrail.transform.position = touchPos;
                currentTrail = null;
                break;
            case TouchPhase.Canceled: // Release the object when touch is canceled.
                currentTrail.transform.position = touchPos;
                currentTrail = null;
                break;
        }
    }
}
