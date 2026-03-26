using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    public static DoorController Instance { get; private set; }

    [Header("Door Open")]
    public float openHeight = 4f;
    public float openDuration = 1.2f;

    [Header("Materials")]
    public Renderer doorRenderer;
    public Color closedColor = new Color(0.3f, 0.3f, 0.3f);
    public Color openColor = new Color(0f, 1f, 0.4f);

    [Header("Exit Trigger")]
    public Collider exitTrigger;

    private bool _isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (doorRenderer != null)
            doorRenderer.material.color = closedColor;

        if (exitTrigger != null)
            exitTrigger.enabled = false;
    }

    public void OpenDoor()
    {
        if (_isOpen) return;
        _isOpen = true;
        StartCoroutine(SlideUp());
    }

    private IEnumerator SlideUp()
    {
        if (doorRenderer != null)
            doorRenderer.material.color = openColor;

        Vector3 start = transform.position;
        Vector3 end = start + Vector3.up * openHeight;
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / openDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;

        // 문이 완전히 열리면 출구 트리거 활성화
        if (exitTrigger != null)
            exitTrigger.enabled = true;
    }
}
