using UnityEngine;

public class TrapSpinner : MonoBehaviour
{
    [Header("Rotation")]
    public Vector3 rotationAxis = Vector3.up;
    public float rotationSpeed = 90f;

    private void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        GameManager.Instance?.GameOver();
    }
}
