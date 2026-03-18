using UnityEngine;

public class AnimationMotionSource : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    
    [Header("Animator Parameters")]
    [SerializeField] private string forwardParameter = "Forward";
    [SerializeField] private string strafeParameter = "Strafe";
    [SerializeField] private string speedParameter = "Speed";

    [Header("Settings")]
    [SerializeField] private float smoothing = 10f;               // скорость сглаживания (ед/с)
    [SerializeField] private float minSpeedThreshold = 0.01f;      // полная остановка, если общая скорость меньше
    [SerializeField] private float minComponentThreshold = 0.02f;  // минимальное значение оси, ниже обнуляем
    [SerializeField] private float relativeThreshold = 0.1f;       // если компонента меньше доли от главной, обнуляем

    private Vector3 _lastPosition;
    private int _velocityXHash;
    private int _velocityZHash;
    private int _speedHash;

    private Vector3 _currentVelocity;
    public Vector3 velocity => _currentVelocity;

    private void Awake()
    {
        _lastPosition = transform.position;
        _velocityXHash = Animator.StringToHash(strafeParameter);
        _velocityZHash = Animator.StringToHash(forwardParameter);
        _speedHash = Animator.StringToHash(speedParameter);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) return;

        Vector3 worldDelta = transform.position - _lastPosition;
        _lastPosition = transform.position;
        Vector3 worldVelocity = worldDelta / deltaTime;

        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);
        

        localVelocity = FilterComponents(localVelocity);

        if (localVelocity.magnitude < minSpeedThreshold)
            localVelocity = Vector3.zero;

        _currentVelocity = Vector3.MoveTowards(
            _currentVelocity,
            localVelocity,
            smoothing * deltaTime
        );

        float speed = _currentVelocity.magnitude;
        if (speed < minSpeedThreshold)
        {
            _currentVelocity = Vector3.zero;
            speed = 0f;
        }

        _animator.SetFloat(_velocityXHash, _currentVelocity.x);
        _animator.SetFloat(_velocityZHash, _currentVelocity.z);
        _animator.SetFloat(_speedHash, speed);
    }
    
    private Vector3 FilterComponents(Vector3 velocity)
    {
        float x = velocity.x;
        float y = velocity.y;
        float z = velocity.z;

        // Шаг 1: Фильтрация по минимальному порогу
        if (Mathf.Abs(x) < minComponentThreshold) x = 0f;
        if (Mathf.Abs(y) < minComponentThreshold) y = 0f;
        if (Mathf.Abs(z) < minComponentThreshold) z = 0f;

        float absX = Mathf.Abs(x);
        float absY = Mathf.Abs(y);
        float absZ = Mathf.Abs(z);

        // Шаг 2: Находим доминирующую компоненту
        float maxAbs = absX;
        float secondMax = absY;
    
        if (absY > maxAbs)
        {
            maxAbs = absY;
            secondMax = Mathf.Max(absX, absZ);
        }
        if (absZ > maxAbs)
        {
            maxAbs = absZ;
            secondMax = Mathf.Max(absX, absY);
        }

        // Шаг 3: Обнуляем недоминирующие компоненты
        if (maxAbs > 0)
        {
            if (absX < maxAbs * relativeThreshold) x = 0f;
            if (absY < maxAbs * relativeThreshold) y = 0f;
            if (absZ < maxAbs * relativeThreshold) z = 0f;
        }

        return new Vector3(x, y, z);
    }
}
