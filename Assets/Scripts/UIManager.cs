using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public TMP_Text timerText;
    public TMP_Text countText;

    [Header("Panels")]
    public GameObject clearPanel;
    public GameObject gameOverPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        clearPanel?.SetActive(false);
        gameOverPanel?.SetActive(false);
    }

    public void UpdateTimer(float seconds)
    {
        if (timerText == null) return;
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        timerText.text = $"{m:00}:{s:00}";

        // 10초 이하 빨간색 강조
        timerText.color = seconds <= 10f ? Color.red : Color.white;
    }

    public void UpdateCount(int collected, int total)
    {
        if (countText != null)
            countText.text = $"BAT  {collected} / {total}";
    }

    public void ShowClearPanel()
    {
        clearPanel?.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowGameOverPanel()
    {
        gameOverPanel?.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
