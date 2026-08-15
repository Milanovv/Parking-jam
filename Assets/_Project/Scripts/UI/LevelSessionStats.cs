using UnityEngine;

public class LevelSessionStats : MonoBehaviour
{
    public float ElapsedPlayTime { get; private set; }

    public int MovesIssued => GameManager.Instance != null ? GameManager.Instance.Tick : 0;

    public void Reset()
    {
        ElapsedPlayTime = 0f;
    }

    private void Update()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null || gameManager.State != GameState.Playing) return;
        ElapsedPlayTime += Time.deltaTime;
    }
}