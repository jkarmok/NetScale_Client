using UnityEngine;

/// <summary>
/// Контролирует анимацию прыжка персонажа в зависимости от его высоты над землей.
/// Адаптируется под любую высоту прыжка автоматически.
/// </summary>
public class JumpAnimationController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    public LayerMask groundLayer;

    [Header("Capsule Check Settings")]
    [SerializeField] private Vector3 offsetCollider = new Vector3(0f, 0.1f, 0f);
    [SerializeField] private float capsuleRadius = 0.3f;
    [SerializeField] private float capsuleHeight = 0.5f;

    [Header("Debug")]
    [SerializeField] private float currentJumpHeight = 0f;
    [SerializeField] private float velocityY = 0f;

    // Авто-калибровка диапазона скоростей
    [SerializeField] private float recordedMaxVelocity = 0f;  // пиковая скорость взлёта
    [SerializeField] private float recordedMinVelocity = 0f;  // пиковая скорость падения

    private AnimationMotionSource _animationMotionSource;
    private static readonly int JumpHeightParam = Animator.StringToHash("JumpHeight");
    private static readonly int JumpAnimationHash = Animator.StringToHash("Monster02_Attack03");

    private void Start()
    {
        _animationMotionSource = GetComponent<AnimationMotionSource>();
    }

    /// <summary>
    /// Запоминает максимальные достигнутые скорости взлёта и падения.
    /// </summary>
    private void CalibrateVelocityRange(float velocity)
    {
        if (velocity > recordedMaxVelocity)
            recordedMaxVelocity = velocity;

        if (velocity < recordedMinVelocity)
            recordedMinVelocity = velocity;
    }

    /// <summary>
    /// Маппинг скорости в диапазон [0, 1]:
    ///   0.0 — начало прыжка (максимальная скорость вверх)
    ///   0.5 — верхняя точка (нулевая скорость)
    ///   1.0 — приземление (максимальная скорость вниз)
    /// </summary>
    private float MapVelocityToRange(float velocity)
    {
        float maxV = Mathf.Max(recordedMaxVelocity, 0.1f);
        float minV = Mathf.Min(recordedMinVelocity, -0.1f);

        return velocity >= 0f
            ? 0.5f * (maxV - velocity) / maxV        // взлёт:  maxV→0  ==  0.0→0.5
            : 0.5f + 0.5f * velocity / minV;         // падение: 0→minV ==  0.5→1.0
    }

    private bool IsGrounded()
    {
        Vector3 point1 = transform.position + offsetCollider + Vector3.up * (capsuleHeight * 0.5f);
        Vector3 point2 = transform.position + offsetCollider - Vector3.up * (capsuleHeight * 0.5f);
        return Physics.CheckCapsule(point1, point2, capsuleRadius, groundLayer);
    }

    private void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _rigidbody.AddForce(Vector3.up * 10f, ForceMode.Impulse);
        }*/
        velocityY = _animationMotionSource.velocity.y;

        if (!IsGrounded())
        {
            CalibrateVelocityRange(velocityY);
            currentJumpHeight = MapVelocityToRange(velocityY);
        }
        else
        {
            currentJumpHeight = 0f;
        }

        _animator.SetFloat(JumpHeightParam, currentJumpHeight);

      //  var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        if (/*stateInfo.fullPathHash == JumpAnimationHash &&*/ currentJumpHeight > 0f)
        {
            _animator.Play(JumpAnimationHash, 0, currentJumpHeight);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 point1 = transform.position + offsetCollider + Vector3.up * (capsuleHeight * 0.5f);
        Vector3 point2 = transform.position + offsetCollider - Vector3.up * (capsuleHeight * 0.5f);
        Gizmos.DrawWireSphere(point1, capsuleRadius);
        Gizmos.DrawWireSphere(point2, capsuleRadius);
        Gizmos.DrawLine(point1, point2);
    }
}