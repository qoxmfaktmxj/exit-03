using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity   = -20f;

    private CharacterController _cc;
    private Animator            _animator;
    private Vector3             _velocity;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int DeadHash  = Animator.StringToHash("Dead");

    private void Awake()
    {
        _cc       = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();   // Mixamo 캐릭터 포함
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameActive()) return;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = transform.right * h + transform.forward * v;
        _cc.Move(move * (moveSpeed * Time.deltaTime));

        // 중력
        if (_cc.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);

        // 애니메이터 Speed 파라미터 업데이트 (Mixamo 연동)
        float speed = new Vector3(h, 0, v).magnitude;
        _animator?.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
    }

    // 장애물 or 게임오버 시 애니메이션 정지
    public void TriggerDead()
    {
        _animator?.SetBool(DeadHash, true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Exit"))
            GameManager.Instance?.GameClear();
    }
}
