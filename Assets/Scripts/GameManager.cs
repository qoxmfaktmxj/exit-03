using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public float timeLimit = 90f;
    public int totalBatteries = 3;

    private float _timeRemaining;
    private int _collectedCount;
    private bool _isGameActive;

    public float TimeRemaining => _timeRemaining;
    public int CollectedCount => _collectedCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        if (!_isGameActive) return;

        _timeRemaining -= Time.deltaTime;
        UIManager.Instance?.UpdateTimer(_timeRemaining);

        if (_timeRemaining <= 0f)
        {
            _timeRemaining = 0f;
            GameOver();
        }
    }

    public void StartGame()
    {
        _timeRemaining = timeLimit;
        _collectedCount = 0;
        _isGameActive = true;
        UIManager.Instance?.UpdateTimer(_timeRemaining);
        UIManager.Instance?.UpdateCount(_collectedCount, totalBatteries);
    }

    public void CollectBattery()
    {
        if (!_isGameActive) return;

        _collectedCount++;
        UIManager.Instance?.UpdateCount(_collectedCount, totalBatteries);

        if (_collectedCount >= totalBatteries)
        {
            DoorController.Instance?.OpenDoor();
        }
    }

    public void GameClear()
    {
        if (!_isGameActive) return;
        _isGameActive = false;
        UIManager.Instance?.ShowClearPanel();
    }

    public void GameOver()
    {
        if (!_isGameActive) return;
        _isGameActive = false;
        FindFirstObjectByType<PlayerController>()?.TriggerDead();
        UIManager.Instance?.ShowGameOverPanel();
    }

    public bool IsGameActive() => _isGameActive;
}
