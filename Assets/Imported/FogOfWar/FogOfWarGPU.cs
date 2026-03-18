using UnityEngine;
using System.Collections.Generic;

namespace FogOfWarSystem
{
    /// <summary>
    /// Универсальная GPU-ускоренная система тумана войны с автоматическим fallback на CPU
    /// Особенности:
    /// - Единый радиус видимости для Entity и тумана
    /// - Плавные переходы затухания
    /// - Автоматическое переключение на CPU версию если нет поддержки Compute Shaders
    /// - Оптимизирована для высокой производительности
    /// </summary>
    public class FogOfWarGPU : MonoBehaviour
    {
        [Header("Map Settings")]
        [SerializeField] private Vector2 mapSize = new Vector2(100f, 100f);
        [SerializeField] private int textureResolution = 2048;
        
        [Header("Fog Settings")]
        [SerializeField] private Color fogColor = new Color(0, 0, 0, 1);
        [SerializeField] private Color exploredColor = new Color(0, 0, 0, 0.5f);
        [SerializeField] private Color visibleColor = new Color(0, 0, 0, 0);
        [SerializeField] private float smoothingRadius = 2f;
        [SerializeField] private float visionDecayRate = 5f;
        [SerializeField] private float falloffExponent = 1.5f; // Контроль плавности затухания (1.0 = линейное, 2.0 = более мягкое)

        [Header("GPU Settings")]
        [SerializeField] private ComputeShader fogComputeShader;
        
        [Header("Performance")]
        [SerializeField] private bool forceUseCPU = false; // Принудительное использование CPU версии для тестирования
        [Header("Transition Smoothness")]
        [Range(0.1f, 5f)]
        [SerializeField] private float transitionSmoothness = 2.0f;
        // Определение типа используемой системы
        private enum SystemType { GPU, CPU }
        private SystemType activeSystem;

        // Render Textures для GPU
        private RenderTexture visionTexture;
        private RenderTexture exploredTexture;
        private RenderTexture outputTexture;
        private RenderTexture blurTempTexture;

        // Compute Shader kernels
        private int kernelClearVision;
        private int kernelUpdateVision;
        private int kernelDecayVision;
        private int kernelBlurVision;
        private int kernelCompositeOutput;

        // Буфер для источников видимости
        private ComputeBuffer visionSourceBuffer;
        private List<VisionSourceGPU> visionSources = new List<VisionSourceGPU>();
        private VisionSourceGPU[] visionSourceArray;

        // CPU fallback
        private FogOfWarCPU cpuFallback;

        // Material для проекции
        private Material fogMaterial;

        // Thread group sizes
        private const int THREAD_GROUP_SIZE = 8;
        private int threadGroupsX;
        private int threadGroupsY;

        public Vector2 MapSize => mapSize;
        public int Resolution => textureResolution;
        public RenderTexture OutputTexture => outputTexture;
        public Texture OutputTextureGeneric => activeSystem == SystemType.GPU ? (Texture)outputTexture : (Texture)cpuFallback?.OutputTexture;
        public bool IsUsingGPU => activeSystem == SystemType.GPU;

        private void Awake()
        {
            DetermineSystemType();
            InitializeSystem();
        }

        private void DetermineSystemType()
        {
            // Проверка поддержки Compute Shaders
            bool supportsComputeShaders = SystemInfo.supportsComputeShaders;
            
            if (forceUseCPU || !supportsComputeShaders || fogComputeShader == null)
            {
                activeSystem = SystemType.CPU;
                Debug.Log("Fog of War: Using CPU implementation");
            }
            else
            {
                activeSystem = SystemType.GPU;
                Debug.Log("Fog of War: Using GPU implementation");
            }
        }

        private void InitializeSystem()
        {
            if (activeSystem == SystemType.GPU)
            {
                InitializeGPUResources();
            }
            else
            {
                InitializeCPUFallback();
            }
        }

        private void InitializeGPUResources()
        {
            // Расчёт thread groups
            threadGroupsX = Mathf.CeilToInt(textureResolution / (float)THREAD_GROUP_SIZE);
            threadGroupsY = Mathf.CeilToInt(textureResolution / (float)THREAD_GROUP_SIZE);

            // Создание RenderTextures
            CreateRenderTextures();

            // Инициализация Compute Shader
            InitializeComputeShader();

            // Создание материала
            CreateFogMaterial();
        }

        private void CreateRenderTextures()
        {
            RenderTextureDescriptor desc = new RenderTextureDescriptor(
                textureResolution, 
                textureResolution, 
                RenderTextureFormat.RFloat,
                0
            );
            desc.enableRandomWrite = true;

            visionTexture = new RenderTexture(desc);
            visionTexture.Create();

            exploredTexture = new RenderTexture(desc);
            exploredTexture.Create();

            blurTempTexture = new RenderTexture(desc);
            blurTempTexture.Create();

            // Output в RGBA для цвета
            desc.colorFormat = RenderTextureFormat.ARGB32;
            outputTexture = new RenderTexture(desc);
            outputTexture.filterMode = FilterMode.Bilinear;
            outputTexture.Create();

            // Очистка начальных текстур
            Graphics.SetRenderTarget(visionTexture);
            GL.Clear(true, true, Color.black);
            Graphics.SetRenderTarget(exploredTexture);
            GL.Clear(true, true, Color.black);
            Graphics.SetRenderTarget(null);
        }

    private void InitializeComputeShader()
{
    if (fogComputeShader == null)
    {
        Debug.LogError("Compute Shader not assigned! Falling back to CPU.");
        activeSystem = SystemType.CPU;
        InitializeCPUFallback();
        return;
    }

    // Получение kernel indices
    kernelClearVision = fogComputeShader.FindKernel("ClearVision");
    kernelUpdateVision = fogComputeShader.FindKernel("UpdateVision");
    kernelDecayVision = fogComputeShader.FindKernel("DecayVision");
    kernelBlurVision = fogComputeShader.FindKernel("BlurVision");
    kernelCompositeOutput = fogComputeShader.FindKernel("CompositeOutput");

    // Установка общих параметров
    fogComputeShader.SetInt("_Resolution", textureResolution);
    fogComputeShader.SetVector("_MapSize", new Vector4(mapSize.x, mapSize.y, 0, 0));
    fogComputeShader.SetFloat("_SmoothRadius", smoothingRadius);
    fogComputeShader.SetFloat("_FalloffExponent", falloffExponent);
    
    // Установка параметра плавности переходов
    fogComputeShader.SetFloat("_TransitionSmoothness", transitionSmoothness);

    // Цвета
    fogComputeShader.SetVector("_FogColor", fogColor);
    fogComputeShader.SetVector("_ExploredColor", exploredColor);
    fogComputeShader.SetVector("_VisibleColor", visibleColor);
    
    // Установка текстур для ядер
    fogComputeShader.SetTexture(kernelBlurVision, "_InputTexture", visionTexture);
    fogComputeShader.SetTexture(kernelBlurVision, "_OutputFloat", blurTempTexture);
    fogComputeShader.SetTexture(kernelCompositeOutput, "_VisionTexture", visionTexture);
    fogComputeShader.SetTexture(kernelCompositeOutput, "_ExploredTexture", exploredTexture);
    fogComputeShader.SetTexture(kernelCompositeOutput, "_OutputTexture", outputTexture);
}

// Обновите метод для установки плавности
public void SetTransitionSmoothness(float smoothness)
{
    transitionSmoothness = Mathf.Clamp(smoothness, 0.1f, 5f);
    if (activeSystem == SystemType.GPU && fogComputeShader != null)
    {
        fogComputeShader.SetFloat("_TransitionSmoothness", transitionSmoothness);
    }
}

        private void InitializeCPUFallback()
        {
            // Создание CPU компонента
            cpuFallback = gameObject.AddComponent<FogOfWarCPU>();
            
            // CPU версия использует свои внутренние настройки из сериализованных полей
            // Материал будет создан CPU компонентом автоматически
        }

        private void CreateFogMaterial()
        {
            Shader fogShader = Shader.Find("Custom/FogOfWarOptimized");
            if (fogShader == null)
            {
                Debug.LogError("Fog of War shader not found!");
                return;
            }

            fogMaterial = new Material(fogShader);
            
            if (activeSystem == SystemType.GPU)
            {
                fogMaterial.SetTexture("_FogTex", outputTexture);
                fogMaterial.SetVector("_MapSize", new Vector4(mapSize.x, mapSize.y, 0, 0));
                ApplyMaterialToMap();
            }
            // CPU версия создаст свой материал сама
        }

        private void ApplyMaterialToMap()
        {
            GameObject fogPlane = GameObject.Find("FogPlane");
            if (fogPlane != null)
            {
                fogPlane.GetComponent<Renderer>().material = fogMaterial;
            }
        }

        private void Update()
        {
            if (activeSystem == SystemType.GPU)
            {
                UpdateFogOfWarGPU();
            }
            // CPU версия обновляется сама через свой Update
        }

        private void UpdateFogOfWarGPU()
        {
            // 1. Затухание видимости
            DecayVision();

            // 2. Обновление от источников
            UpdateVisionFromSources();

            // 3. Применение blur
            ApplyBlur();

            // 4. Композиция финального изображения
            CompositeOutput();
        }

        private void DecayVision()
        {
            fogComputeShader.SetFloat("_DecayRate", visionDecayRate * Time.deltaTime);
            fogComputeShader.SetTexture(kernelDecayVision, "_VisionTexture", visionTexture);
            fogComputeShader.Dispatch(kernelDecayVision, threadGroupsX, threadGroupsY, 1);
        }

        private void UpdateVisionFromSources()
        {
            if (visionSources.Count == 0) return;

            // Обновление буфера источников
            UpdateVisionSourceBuffer();

            // Dispatch compute shader
            fogComputeShader.SetBuffer(kernelUpdateVision, "_VisionSources", visionSourceBuffer);
            fogComputeShader.SetInt("_SourceCount", visionSources.Count);
            fogComputeShader.SetTexture(kernelUpdateVision, "_VisionTexture", visionTexture);
            fogComputeShader.SetTexture(kernelUpdateVision, "_ExploredTexture", exploredTexture);
            
            fogComputeShader.Dispatch(kernelUpdateVision, threadGroupsX, threadGroupsY, 1);
        }

        private void UpdateVisionSourceBuffer()
        {
            // Пересоздание буфера если размер изменился
            if (visionSourceBuffer == null || visionSourceBuffer.count != visionSources.Count)
            {
                visionSourceBuffer?.Release();
                
                if (visionSources.Count > 0)
                {
                    visionSourceBuffer = new ComputeBuffer(visionSources.Count, sizeof(float) * 4);
                }
            }

            if (visionSources.Count > 0)
            {
                // Обновление данных
                if (visionSourceArray == null || visionSourceArray.Length != visionSources.Count)
                {
                    visionSourceArray = new VisionSourceGPU[visionSources.Count];
                }

                for (int i = 0; i < visionSources.Count; i++)
                {
                    visionSourceArray[i] = visionSources[i];
                }

                visionSourceBuffer.SetData(visionSourceArray);
            }
        }

        private void ApplyBlur()
        {
            // Горизонтальный blur
            fogComputeShader.SetTexture(kernelBlurVision, "_InputTexture", visionTexture);
            fogComputeShader.SetTexture(kernelBlurVision, "_OutputFloat", blurTempTexture);
            fogComputeShader.SetInt("_BlurDirection", 0);
            fogComputeShader.Dispatch(kernelBlurVision, threadGroupsX, threadGroupsY, 1);

            // Вертикальный blur
            fogComputeShader.SetTexture(kernelBlurVision, "_InputTexture", blurTempTexture);
            fogComputeShader.SetTexture(kernelBlurVision, "_OutputFloat", visionTexture);
            fogComputeShader.SetInt("_BlurDirection", 1);
            fogComputeShader.Dispatch(kernelBlurVision, threadGroupsX, threadGroupsY, 1);
        }

        private void CompositeOutput()
        {
            fogComputeShader.SetTexture(kernelCompositeOutput, "_VisionTexture", visionTexture);
            fogComputeShader.SetTexture(kernelCompositeOutput, "_ExploredTexture", exploredTexture);
            fogComputeShader.SetTexture(kernelCompositeOutput, "_OutputTexture", outputTexture);
            
            fogComputeShader.Dispatch(kernelCompositeOutput, threadGroupsX, threadGroupsY, 1);
        }

        public int RegisterVisionSource(Vector3 worldPosition, float radius)
        {
            if (activeSystem == SystemType.CPU && cpuFallback != null)
            {
                cpuFallback.RegisterVisionSource(worldPosition, radius);
                return visionSources.Count; // Возвращаем индекс для совместимости
            }

            Vector2Int texCoords = WorldToTextureCoords(worldPosition);
            float radiusInTexels = (radius / mapSize.x) * textureResolution;

            VisionSourceGPU source = new VisionSourceGPU
            {
                x = texCoords.x / (float)textureResolution,
                y = texCoords.y / (float)textureResolution,
                radius = radiusInTexels / textureResolution,
                intensity = 1f
            };

            visionSources.Add(source);
            return visionSources.Count - 1; // Возвращаем индекс
        }

        public void UnregisterVisionSource(int index)
        {
            if (activeSystem == SystemType.CPU && cpuFallback != null)
            {
                // CPU версия управляет своими источниками
                return;
            }
            
            if (index < 0 || index >= visionSources.Count) return;


            visionSources.RemoveAt(index);
        }

        public void ClearAllSources()
        {
            if (activeSystem == SystemType.CPU && cpuFallback != null)
            {
                cpuFallback.ClearAllSources();
                return;
            }

            visionSources.Clear();
        }

        public void UpdateSourcePosition(int index, Vector3 worldPosition)
        {
            if (activeSystem == SystemType.CPU && cpuFallback != null)
            {
                cpuFallback.UpdateSourcePosition(index, worldPosition);
                return;
            }

            if (index < 0 || index >= visionSources.Count) return;
            
            Vector2Int texCoords = WorldToTextureCoords(worldPosition);
            VisionSourceGPU source = visionSources[index];
            source.x = texCoords.x / (float)textureResolution;
            source.y = texCoords.y / (float)textureResolution;
            visionSources[index] = source;
        }

        public void UpdateSourceRadius(int index, float radius)
        {
            if (activeSystem == SystemType.CPU && cpuFallback != null)
            {
                // CPU версия пока не поддерживает динамическое изменение радиуса
                return;
            }
            
            if (index < 0 || index >= visionSources.Count) return;

            float radiusInTexels = (radius / mapSize.x) * textureResolution;
            VisionSourceGPU source = visionSources[index];
            source.radius = radiusInTexels / textureResolution;
            visionSources[index] = source;
        }

        public Vector2Int WorldToTextureCoords(Vector3 worldPosition)
        {
            float normalizedX = (worldPosition.x + mapSize.x * 0.5f) / mapSize.x;
            float normalizedZ = (worldPosition.z + mapSize.y * 0.5f) / mapSize.y;

            int texX = Mathf.Clamp((int)(normalizedX * textureResolution), 0, textureResolution - 1);
            int texY = Mathf.Clamp((int)(normalizedZ * textureResolution), 0, textureResolution - 1);

            return new Vector2Int(texX, texY);
        }
        [Header("Entity Visibility")]
        [SerializeField] private float entityVisibilityRadiusMultiplier = 1.2f; // Умножаем радиус источников при проверке видимости сущностей

        public bool IsPositionVisible(Vector3 worldPosition, float threshold = 0.3f)
        {
            // Проверка выхода за границы карты
            if (worldPosition.x < -mapSize.x * 0.5f || worldPosition.x > mapSize.x * 0.5f ||
                worldPosition.z < -mapSize.y * 0.5f || worldPosition.z > mapSize.y * 0.5f)
            {
                return false;
            }

            if (activeSystem == SystemType.CPU && cpuFallback != null)
            {
                return cpuFallback.IsPositionVisible(worldPosition, threshold);
            }

            // 1. Проверка по текстуре (стандартный метод)
            Vector2Int texCoords = WorldToTextureCoords(worldPosition);
    
            RenderTexture.active = visionTexture;
            Texture2D tempTex = new Texture2D(1, 1, TextureFormat.RFloat, false);
            tempTex.ReadPixels(new Rect(texCoords.x, texCoords.y, 1, 1), 0, 0);
            tempTex.Apply();
            RenderTexture.active = null;
    
            float visibility = tempTex.GetPixel(0, 0).r;
            Destroy(tempTex);
    
            if (visibility > threshold)
                return true;
    
            // 2. Дополнительная проверка по расширенным радиусам источников (только для GPU системы)
            float normalizedX = (worldPosition.x + mapSize.x * 0.5f) / mapSize.x;
            float normalizedZ = (worldPosition.z + mapSize.y * 0.5f) / mapSize.y;
    
            foreach (var source in visionSources)
            {
                float dx = normalizedX - source.x;
                float dz = normalizedZ - source.y;
                float distanceSqr = dx * dx + dz * dz;
        
                // Расширенный радиус для проверки видимости сущностей
                float extendedRadius = source.radius * entityVisibilityRadiusMultiplier;
        
                if (distanceSqr <= extendedRadius * extendedRadius)
                {
                    return true;
                }
            }
    
            return false;
        }
        private void OnDestroy()
        {
            if (activeSystem == SystemType.GPU)
            {
                // Освобождение GPU ресурсов
                if (visionTexture != null) visionTexture.Release();
                if (exploredTexture != null) exploredTexture.Release();
                if (outputTexture != null) outputTexture.Release();
                if (blurTempTexture != null) blurTempTexture.Release();
                
                visionSourceBuffer?.Release();
            }
            
            if (fogMaterial != null) Destroy(fogMaterial);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(mapSize.x, 0.1f, mapSize.y));

            // Визуализация источников видимости
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            foreach (var source in visionSources)
            {
                // Конвертация обратно в world space для отображения
                float worldX = source.x * mapSize.x - mapSize.x * 0.5f;
                float worldZ = source.y * mapSize.y - mapSize.y * 0.5f;
                float worldRadius = source.radius * mapSize.x;
                
                Gizmos.DrawWireSphere(new Vector3(worldX, transform.position.y, worldZ), worldRadius);
            }
        }

        // Публичные свойства для настройки
        public void SetFalloffExponent(float exponent)
        {
            falloffExponent = Mathf.Max(0.5f, exponent);
            if (activeSystem == SystemType.GPU && fogComputeShader != null)
            {
                fogComputeShader.SetFloat("_FalloffExponent", falloffExponent);
            }
        }

        public void SetVisionDecayRate(float rate)
        {
            visionDecayRate = Mathf.Max(0f, rate);
        }

        public void SetSmoothingRadius(float radius)
        {
            smoothingRadius = Mathf.Max(0f, radius);
            if (activeSystem == SystemType.GPU && fogComputeShader != null)
            {
                fogComputeShader.SetFloat("_SmoothRadius", smoothingRadius);
            }
        }
    }

    /// <summary>
    /// Структура данных источника видимости для GPU
    /// Должна совпадать со структурой в Compute Shader
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct VisionSourceGPU
    {
        public float x;          // Normalized position X (0-1)
        public float y;          // Normalized position Y (0-1)
        public float radius;     // Normalized radius (0-1)
        public float intensity;  // Vision strength (0-1)
    }
}