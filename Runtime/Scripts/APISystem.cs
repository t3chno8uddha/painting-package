using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using com.appidea.MiniGamePlatform.CommunicationAPI;

public class APISystem : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private PaintManager levelManager;
    [SerializeField] private Slider exitSlider;
    private PaintGameEntryPoint entryPoint;
    private bool isDraggingSlider;

    private IGameOverScreen gameOverScreen;
    [SerializeField] private Canvas mainCanvas;
    public Canvas MainCanvas => mainCanvas;

    private void OnEnable() => levelManager.OnGameFinish += ShowGameOverScreenIfExist;
    private void OnDisable() => levelManager.OnGameFinish -= ShowGameOverScreenIfExist;
    public void SetEntryPoint(PaintGameEntryPoint yourEntryPointClass) =>
        entryPoint = yourEntryPointClass;

    public void SetGameOverScreen(IGameOverScreen screen)
    {
        gameOverScreen = screen;
        gameOverScreen.RunNextClicked += entryPoint.SendGameFinishedAndRunNext;
    }

    private void OnDestroy()
    {
        if (gameOverScreen != null)
            gameOverScreen.RunNextClicked -= entryPoint.SendGameFinishedAndRunNext;
    }

    private void ShowGameOverScreenIfExist()
    {
        if (gameOverScreen == null)
        {
            entryPoint.SendGameFinished();
            return;
        }
        gameOverScreen.ShowGameOverScreen();
        StartCoroutine(SliderLastChildTimer()); ;
    }

    private IEnumerator SliderLastChildTimer()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        exitSlider.transform.SetAsLastSibling();
    }
    private void Update()
    {
        var isInputReleased = Input.GetMouseButtonUp(0) ||
                              Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended;

        if (isDraggingSlider || !isInputReleased) return;
        if (exitSlider.value <= 0.3f)
        {
            entryPoint.SendGameFinished();
        }
        else
        {
            StartCoroutine(ResetSliderSmoothly());
        }
    }

    public void OnBeginDrag(PointerEventData eventData) => isDraggingSlider = true;
    public void OnEndDrag(PointerEventData eventData) => isDraggingSlider = false;

    private IEnumerator ResetSliderSmoothly()
    {
        const float duration = 0.1f;
        var elapsedTime = 0f;
        var startValue = exitSlider.value;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            exitSlider.value = Mathf.Lerp(startValue, 1, elapsedTime / duration);
            yield return null;
        }

        exitSlider.value = 1;
    }
}
