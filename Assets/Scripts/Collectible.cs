using UnityEngine;

public class Collectible : MonoBehaviour
{
    [Header("Rotation")]
    public float rotateSpeed = 90f;

    [Header("Bob")]
    public float bobHeight = 0.3f;
    public float bobSpeed = 2f;

    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.position;
    }

    private void Update()
    {
        // 둥둥 떠다니는 효과
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        float newY = _startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        GameManager.Instance?.CollectBattery();
        gameObject.SetActive(false);
    }
}
