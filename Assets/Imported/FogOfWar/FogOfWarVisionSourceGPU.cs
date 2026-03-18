using UnityEngine;

namespace FogOfWarSystem
{
    /// <summary>
    /// Улучшенный источник видимости с единым радиусом
    /// Автоматически регистрируется в FogOfWar системе (GPU или CPU)
    /// Поддерживает динамическое добавление/удаление и изменение параметров
    /// </summary>
    public class FogOfWarVisionSourceGPU : MonoBehaviour
    {
        [Header("Vision Settings")]
        [SerializeField] private float visionRadius = 10f;
        [SerializeField] private bool isActiveOnStart = true;
        [SerializeField] private bool updateEveryFrame = true;

        [Header("Runtime")]
        [SerializeField] private bool isActive;

        private FogOfWarGPU fogOfWarGPU;
        private Transform cachedTransform;
        private int sourceIndex = -1;
        private Vector3 lastPosition;
        private float lastRadius;
        private const float POSITION_UPDATE_THRESHOLD = 0.01f;
        private const float RADIUS_UPDATE_THRESHOLD = 0.1f;

        public float VisionRadius => visionRadius;
        public bool IsActive => isActive && gameObject.activeInHierarchy;
        public int SourceIndex => sourceIndex;

        private void Awake()
        {
            cachedTransform = transform;
            isActive = isActiveOnStart;
            lastPosition = cachedTransform.position;
            lastRadius = visionRadius;
        }

        private void Start()
        {
            FindAndRegisterToFogOfWar();
        }

        private void FindAndRegisterToFogOfWar()
        {
            fogOfWarGPU = FindObjectOfType<FogOfWarGPU>();

            if (fogOfWarGPU == null)
            {
                Debug.LogWarning($"FogOfWarGPU system not found for {gameObject.name}");
                return;
            }

            if (isActive)
            {
                RegisterSource();
            }
        }

        private void RegisterSource()
        {
            if (fogOfWarGPU != null && sourceIndex == -1)
            {
                sourceIndex = fogOfWarGPU.RegisterVisionSource(cachedTransform.position, visionRadius);
                lastPosition = cachedTransform.position;
                lastRadius = visionRadius;
            }
        }

        private void UnregisterSource()
        {
            if (fogOfWarGPU != null && sourceIndex != -1)
            {
                fogOfWarGPU.UnregisterVisionSource(sourceIndex);
                sourceIndex = -1;
            }
        }

        private void Update()
        {
            if (!isActive || !updateEveryFrame || fogOfWarGPU == null || sourceIndex == -1)
                return;

            bool needsUpdate = false;

            // Проверка изменения позиции
            Vector3 currentPos = cachedTransform.position;
            
            if (Vector3.SqrMagnitude(currentPos - lastPosition) > POSITION_UPDATE_THRESHOLD * POSITION_UPDATE_THRESHOLD)
            {
                fogOfWarGPU.UpdateSourcePosition(sourceIndex, currentPos);
                lastPosition = currentPos;
                needsUpdate = true;
            }

            // Проверка изменения радиуса
            if (Mathf.Abs(visionRadius - lastRadius) > RADIUS_UPDATE_THRESHOLD)
            {
                fogOfWarGPU.UpdateSourceRadius(sourceIndex, visionRadius);
                lastRadius = visionRadius;
                needsUpdate = true;
            }
        }

        /// <summary>
        /// Включить/выключить источник видимости
        /// </summary>
        public void SetActive(bool active)
        {
            if (isActive == active) return;

            isActive = active;

            if (active)
            {
                RegisterSource();
            }
            else
            {
                UnregisterSource();
            }
        }

        /// <summary>
        /// Установить радиус видимости
        /// </summary>
        public void SetVisionRadius(float radius)
        {
            visionRadius = Mathf.Max(0f, radius);
            
            // Обновление радиуса в системе если источник активен
            if (isActive && fogOfWarGPU != null && sourceIndex != -1)
            {
                fogOfWarGPU.UpdateSourceRadius(sourceIndex, visionRadius);
                lastRadius = visionRadius;
            }
        }

        /// <summary>
        /// Принудительное обновление позиции (полезно при телепортации)
        /// </summary>
        public void ForcePositionUpdate()
        {
            if (isActive && fogOfWarGPU != null && sourceIndex != -1)
            {
                fogOfWarGPU.UpdateSourcePosition(sourceIndex, cachedTransform.position);
                lastPosition = cachedTransform.position;
            }
        }

        private void OnEnable()
        {
            // Если объект был выключен и включен заново
            if (isActive && fogOfWarGPU != null && sourceIndex == -1)
            {
                RegisterSource();
            }
        }

        private void OnDisable()
        {
            // Временное отключение источника
            if (sourceIndex != -1)
            {
                UnregisterSource();
            }
        }

        private void OnDestroy()
        {
            // Полное удаление источника
            UnregisterSource();
        }

        private void OnDrawGizmosSelected()
        {
            // Визуализация радиуса видимости
            Gizmos.color = IsActive ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
            Gizmos.DrawSphere(transform.position, visionRadius);

            Gizmos.color = IsActive ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, visionRadius);
            
            // Отображение текущего состояния
            if (IsActive && Application.isPlaying)
            {
                // Внутренний радиус - область полной видимости
                Gizmos.color = new Color(0, 1, 0, 0.5f);
                Gizmos.DrawWireSphere(transform.position, visionRadius * 0.5f);
            }
        }

        // Editor-only: визуализация в сцене
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                // Простая визуализация в редакторе
                Gizmos.color = new Color(0, 1, 0, 0.1f);
                Gizmos.DrawSphere(transform.position, visionRadius);
            }
        }
    }
}