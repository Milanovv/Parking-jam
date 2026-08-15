using UnityEngine;

public class PauseController : MonoBehaviour
{
    public bool IsPaused { get; private set; }

    public GameObject PausePanel { get; set; }

    public void ShowPause()
    {
        IsPaused = true;
        if (GameManager.Instance != null) GameManager.Instance.Pause();
        if (PausePanel != null) PausePanel.SetActive(true);
    }

    public void Resume()
    {
        IsPaused = false;
        if (GameManager.Instance != null) GameManager.Instance.Resume();
        if (PausePanel != null) PausePanel.SetActive(false);
    }
}
