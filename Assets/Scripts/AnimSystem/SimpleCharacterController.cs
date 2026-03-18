using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleCharacterController : MonoBehaviour
{
    [Header("Настройки движения")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;
    
    [Header("Настройки игрока")]
    public KeyCode runKey = KeyCode.LeftShift;
    
    private CharacterController controller;
    private Vector3 velocity; // Теперь это ПОЛНАЯ скорость персонажа!
    private bool isGrounded;
    
    // Свойство для доступа к скорости из аниматора (опционально)
    public Vector3 Velocity => velocity;
    
    public float Speed => velocity.magnitude;
    public float HorizontalSpeed => new Vector3(velocity.x, 0, velocity.z).magnitude;
    public float VerticalSpeed => velocity.y;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }
    
    void Update()
    {
        // Проверяем, стоит ли персонаж на земле
        isGrounded = controller.isGrounded;
        
        // Если на земле и падаем вниз, сбрасываем вертикальную скорость
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        // ПОЛУЧАЕМ ВВОД
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Создаем вектор движения в локальных координатах
        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        
        // Нормализуем диагональное движение (чтобы по диагонали не бежать быстрее)
        if (move.magnitude > 1f)
        {
            move.Normalize();
        }
        
        // Определяем текущую скорость
        float currentSpeed = Input.GetKey(runKey) ? runSpeed : walkSpeed;
        
        // УСТАНАВЛИВАЕМ ГОРИЗОНТАЛЬНУЮ СКОРОСТЬ В velocity
        // Теперь velocity.x и velocity.z содержат скорость движения
        velocity.x = move.x * currentSpeed;
        velocity.z = move.z * currentSpeed;
        
        // ПРЫЖОК - изменяем вертикальную скорость
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        
        // ГРАВИТАЦИЯ - применяем к вертикальной скорости
        velocity.y += gravity * Time.deltaTime;
        
        // ПРИМЕНЯЕМ ВСЁ ДВИЖЕНИЕ СРАЗУ
        // Используем velocity * Time.deltaTime для перемещения
        controller.Move(velocity * Time.deltaTime);
        
        // ДЛЯ АНИМАЦИЙ: здесь можно передавать значения в аниматор
        // Например:
        // Animator anim = GetComponent<Animator>();
        // anim.SetFloat("Speed", velocity.magnitude);
        // anim.SetFloat("HorizontalSpeed", new Vector3(velocity.x, 0, velocity.z).magnitude);
        // anim.SetFloat("VerticalSpeed", velocity.y);
        // anim.SetBool("IsGrounded", isGrounded);
    }
#if UNITY_EDITOR
    // Опционально: для отладки - рисуем скорость в Scene view
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position + Vector3.up, velocity);
            
            // Рисуем текст со значением скорости
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
                $"Speed: {velocity.magnitude:F2}\nVel Y: {velocity.y:F2}");
        }
    }
#endif
 
}