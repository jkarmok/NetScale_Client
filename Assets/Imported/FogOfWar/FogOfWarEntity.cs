using UnityEngine;

namespace FogOfWarSystem
{
    /// <summary>
    /// Улучшенный компонент для управления видимостью объектов в тумане войны
    /// Использует единый радиус видимости и оптимизирован для минимизации вызовов
    /// </summary>
    public class FogOfWarEntity : MonoBehaviour
    {
        public enum VisibilityMode
        {
            Hide,           // Полностью скрывать в тумане
            ShowAsExplored, // Показывать в исследованных областях с затемнением
            AlwaysVisible   // Всегда видимый (для terrain, структур)
        }

        [Header("Visibility Settings")]
        [SerializeField] private VisibilityMode visibilityMode = VisibilityMode.Hide;
        [SerializeField] private int teamId = 0; // Для какой команды объект виден
        [SerializeField] private float checkInterval = 0.2f; // Интервал проверки видимости
        [SerializeField] private float visibilityThreshold = 0.3f; // Порог видимости (0-1)

        [Header("Performance")]
        [SerializeField] private bool useDistanceCulling = true; // Оптимизация: не проверять далёкие объекты
        [SerializeField] private float maxCheckDistance = 100f; // Максимальное расстояние для проверки

        [Header("Components")]
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Canvas[] canvases;
        [SerializeField] private Collider[] colliders; // Опционально: отключать коллайдеры невидимых объектов

        private FogOfWarGPU fogOfWar;
        private Transform cachedTransform;
        private float timeSinceLastCheck;
        private bool isCurrentlyVisible = true;
 

        // Кэш для камеры (для distance culling)
        private Camera mainCamera;
        private bool hasMainCamera;
 
        private void Awake()
        {
            cachedTransform = transform;
     
            // Автоматический поиск Renderer компонентов если не назначены
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }

            if (canvases == null || canvases.Length == 0)
            {
                canvases = GetComponentsInChildren<Canvas>();
            }

            if (colliders == null || colliders.Length == 0)
            {
                colliders = GetComponentsInChildren<Collider>();
            }

            // Кэширование камеры для distance culling
            if (useDistanceCulling)
            {
                mainCamera = Camera.main;
                hasMainCamera = mainCamera != null;
            }
        }

        private void Start()
        {
            fogOfWar = FindObjectOfType<FogOfWarGPU>();
            
            if (fogOfWar == null)
            {
                Debug.LogWarning($"FogOfWar system not found for {gameObject.name}");
                enabled = false;
                return;
            }

            // Немедленная проверка видимости
            CheckVisibility();
        }

        private void Update()
        {
            timeSinceLastCheck += Time.deltaTime;

            if (timeSinceLastCheck >= checkInterval)
            {
                CheckVisibility();
                timeSinceLastCheck = 0f;
            }
        }

        private void CheckVisibility()
        {
            if (fogOfWar == null) return;

            // Оптимизация: distance culling
            if (useDistanceCulling && hasMainCamera)
            {
                float distanceToCamera = Vector3.Distance(cachedTransform.position, mainCamera.transform.position);
                if (distanceToCamera > maxCheckDistance)
                {
                    // Слишком далеко от камеры - скрываем
                    if (isCurrentlyVisible)
                    {
                        UpdateVisibility(false);
                        isCurrentlyVisible = false;
                    }
                    return;
                }
            }

            bool shouldBeVisible = DetermineVisibility();

            if (shouldBeVisible != isCurrentlyVisible)
            {
                UpdateVisibility(shouldBeVisible);
                isCurrentlyVisible = shouldBeVisible;
            }
        }

        private bool DetermineVisibility()
        {
            switch (visibilityMode)
            {
                case VisibilityMode.AlwaysVisible:
                    return true;

                case VisibilityMode.Hide:
                    // Используем настраиваемый порог видимости
                    return fogOfWar.IsPositionVisible(cachedTransform.position, visibilityThreshold);

                case VisibilityMode.ShowAsExplored:
                    // В режиме ShowAsExplored всегда показываем, но можем применять разную прозрачность
                    // Это требует дополнительной логики с MaterialPropertyBlock
                    return true;

                default:
                    return false;
            }
        }

        private void UpdateVisibility(bool visible)
        {
            // Обновление Renderer компонентов
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = visible;
                }
            }

            // Обновление Canvas компонентов (для UI над объектами)
            foreach (var canvas in canvases)
            {
                if (canvas != null)
                {
                    canvas.enabled = visible;
                }
            }

            // Опционально: отключение коллайдеров для невидимых объектов
            // Это может улучшить производительность физики
            if (colliders != null && colliders.Length > 0)
            {
                foreach (var col in colliders)
                {
                    if (col != null)
                    {
                        col.enabled = visible;
                    }
                }
            }
        }

        /// <summary>
        /// Принудительная проверка видимости (вызывается извне при необходимости)
        /// </summary>
        public void ForceVisibilityCheck()
        {
            CheckVisibility();
        }

        /// <summary>
        /// Установить режим видимости
        /// </summary>
        public void SetVisibilityMode(VisibilityMode mode)
        {
            if (visibilityMode != mode)
            {
                visibilityMode = mode;
                CheckVisibility();
            }
        }

        /// <summary>
        /// Установить порог видимости
        /// </summary>
        public void SetVisibilityThreshold(float threshold)
        {
            visibilityThreshold = Mathf.Clamp01(threshold);
            CheckVisibility();
        }

        /// <summary>
        /// Получить текущее состояние видимости
        /// </summary>
        public bool IsVisible => isCurrentlyVisible;

        /// <summary>
        /// Установить интервал проверки видимости
        /// Больший интервал = лучше производительность, но менее отзывчивая видимость
        /// </summary>
        public void SetCheckInterval(float interval)
        {
            checkInterval = Mathf.Max(0.05f, interval);
        }

        private void OnDrawGizmosSelected()
        {
            // Визуализация состояния видимости
            Gizmos.color = isCurrentlyVisible ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Визуализация distance culling радиуса
            if (useDistanceCulling)
            {
                Gizmos.color = new Color(1, 1, 0, 0.3f);
                Gizmos.DrawWireSphere(transform.position, maxCheckDistance);
            }
        }

        private void OnValidate()
        {
            // Проверка валидности параметров в редакторе
            checkInterval = Mathf.Max(0.05f, checkInterval);
            visibilityThreshold = Mathf.Clamp01(visibilityThreshold);
            maxCheckDistance = Mathf.Max(1f, maxCheckDistance);
        }
    }
}