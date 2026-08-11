using UnityEngine;

public abstract class MiniGameController : MonoBehaviour
{
    public abstract void Retry();

    protected void CompleteWin()
    {
        MiniGameManager.EnsureInstance().CompleteMiniGame();
    }

    protected static Canvas BuildCanvas()
    {
        var host = new GameObject("MiniGameCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas = host.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = host.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 1f;
        return canvas;
    }

    protected static void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) return;
        var host = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        host.tag = "Untagged";
    }
}