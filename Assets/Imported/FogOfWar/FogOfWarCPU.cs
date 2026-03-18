using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;

namespace FogOfWarSystem
{
    /// <summary>
    /// Оптимизированная CPU версия Fog of War с Jobs System
    /// </summary>
    public class FogOfWarCPU : MonoBehaviour
    {
        [Header("Map Settings")]
        [SerializeField] private Vector2 mapSize = new Vector2(100f, 100f);
        [SerializeField] private int textureResolution = 512;
        
        [Header("Fog Settings")]
        [SerializeField] private Color32 fogColor = new Color32(0, 0, 0, 255);
        [SerializeField] private Color32 exploredColor = new Color32(0, 0, 0, 128);
        [SerializeField] private Color32 visibleColor = new Color32(0, 0, 0, 0);
        [SerializeField] private float visionDecayRate = 0.5f;
        
        [Header("Performance")]
        [SerializeField] private bool useJobs = true;
        [SerializeField] private int jobBatchSize = 64;

        // Текстуры
        private Texture2D outputTexture;
        
        // Native массивы
        private NativeArray<float> visionData;
        private NativeArray<float> exploredData;
        private NativeArray<Color32> outputData;
        private NativeArray<VisionSource> sourcesArray;
        
        // Источники
        private List<VisionSource> visionSources = new List<VisionSource>();
        
        // Material
        private Material fogMaterial;
        
        // Job handles
        private JobHandle currentJobHandle;
        private bool jobScheduled = false;
        
        // Отладка
        private List<Vector3> debugWorldPositions = new List<Vector3>();
        private List<float> debugRadii = new List<float>();

        public Texture2D OutputTexture => outputTexture;

        [System.Serializable]
        public struct VisionSource
        {
            public float2 position;    // Нормализованные координаты (0-1)
            public float radius;       // Нормализованный радиус (0-1)
        }

        void Awake()
        {
            Initialize();
        }

        void Initialize()
        {
            int totalPixels = textureResolution * textureResolution;

            // Создаем Native массивы
            visionData = new NativeArray<float>(totalPixels, Allocator.Persistent);
            exploredData = new NativeArray<float>(totalPixels, Allocator.Persistent);
            outputData = new NativeArray<Color32>(totalPixels, Allocator.Persistent);
            sourcesArray = new NativeArray<VisionSource>(0, Allocator.Persistent);

            // Инициализируем данные
            for (int i = 0; i < totalPixels; i++)
            {
                visionData[i] = 0f;
                exploredData[i] = 0f;
                outputData[i] = fogColor;
            }

            // Создаем текстуру
            outputTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
            outputTexture.filterMode = FilterMode.Bilinear;
            outputTexture.wrapMode = TextureWrapMode.Clamp;

            // Создаем материал
            CreateFogMaterial();
            
            // Применяем начальную текстуру
            ApplyToTexture();
            
            Debug.Log($"FogOfWarCPU initialized: {textureResolution}x{textureResolution}, total pixels: {totalPixels}");
        }

        void CreateFogMaterial()
        {
            Shader fogShader = Shader.Find("Custom/FogOfWarOptimized");
            if (fogShader == null)
            {
                Debug.LogError("Fog shader not found!");
                return;
            }

            fogMaterial = new Material(fogShader);
            fogMaterial.SetTexture("_FogTex", outputTexture);
            fogMaterial.SetVector("_MapSize", new Vector4(mapSize.x, mapSize.y, 0, 0));

            // Применяем материал к плоскости
            ApplyMaterialToMap();
        }

        void ApplyMaterialToMap()
        {
            GameObject fogPlane = GameObject.Find("FogPlane");
            if (fogPlane != null)
            {
                fogPlane.GetComponent<Renderer>().material = fogMaterial;
                Debug.Log("Fog material applied to FogPlane");
            }
            else
            {
                Debug.LogWarning("FogPlane not found in scene!");
            }
        }

        void Update()
        {
            // Завершаем предыдущий job если есть
            if (jobScheduled)
            {
                currentJobHandle.Complete();
                jobScheduled = false;
            }

            // Обновляем туман
            UpdateFog();
        }

        void UpdateFog()
        {
            if (visionSources.Count == 0)
            {
                // Если нет источников, просто затухаем
                SimpleDecay();
                return;
            }

            if (useJobs)
            {
                UpdateWithJobs();
            }
            else
            {
                UpdateWithoutJobs();
            }
        }

        void UpdateWithJobs()
        {
            // Обновляем массив источников
            UpdateSourcesArray();

            // 1. Job для затухания
            var decayJob = new DecayJob
            {
                visionData = visionData,
                exploredData = exploredData,
                decayRate = visionDecayRate * Time.deltaTime
            };
            
            JobHandle decayHandle = decayJob.Schedule(visionData.Length, jobBatchSize);

            // 2. Job для обновления видимости от источников
            var updateJob = new UpdateVisionJob
            {
                visionData = visionData,
                exploredData = exploredData,
                sources = sourcesArray,
                resolution = textureResolution,
                invResolution = 1f / textureResolution
            };
            
            JobHandle updateHandle = updateJob.Schedule(textureResolution, 8, decayHandle);

            // 3. Job для композиции текстуры
            var compositeJob = new CompositeJob
            {
                visionData = visionData,
                exploredData = exploredData,
                outputData = outputData,
                fogColor = fogColor,
                exploredColor = exploredColor,
                visibleColor = visibleColor
            };
            
            currentJobHandle = compositeJob.Schedule(outputData.Length, jobBatchSize, updateHandle);
            jobScheduled = true;

            // Ждем завершения и применяем
            currentJobHandle.Complete();
            ApplyToTexture();
        }

        void UpdateWithoutJobs()
        {
            // Простое обновление без jobs для отладки
            SimpleDecay();
            UpdateVisionFromSourcesCPU();
            CompositeOutputCPU();
            ApplyToTexture();
        }

        void SimpleDecay()
        {
            for (int i = 0; i < visionData.Length; i++)
            {
                visionData[i] = math.max(0f, visionData[i] - visionDecayRate * Time.deltaTime);
                exploredData[i] = math.max(0f, exploredData[i] - visionDecayRate * Time.deltaTime * 0.1f);
            }
        }

        void UpdateVisionFromSourcesCPU()
        {
            float invRes = 1f / textureResolution;
            
            for (int y = 0; y < textureResolution; y++)
            {
                for (int x = 0; x < textureResolution; x++)
                {
                    int index = y * textureResolution + x;
                    float pixelX = (x + 0.5f) * invRes;
                    float pixelY = (y + 0.5f) * invRes;
                    
                    float maxVision = visionData[index];
                    
                    foreach (var source in visionSources)
                    {
                        float dx = pixelX - source.position.x;
                        float dy = pixelY - source.position.y;
                        float distanceSqr = dx * dx + dy * dy;
                        
                        if (distanceSqr <= source.radius * source.radius)
                        {
                            float distance = math.sqrt(distanceSqr);
                            float normalizedDist = distance / source.radius;
                            float visibility = 1f - normalizedDist;
                            
                            maxVision = math.max(maxVision, visibility);
                            
                            // Обновляем explored
                            if (visibility > 0.1f)
                            {
                                exploredData[index] = math.max(exploredData[index], visibility * 0.3f);
                            }
                        }
                    }
                    
                    visionData[index] = maxVision;
                }
            }
        }

        void CompositeOutputCPU()
        {
            for (int i = 0; i < outputData.Length; i++)
            {
                float vision = visionData[i];
                float explored = exploredData[i];

                Color32 color;
                
                if (vision > 0.1f)
                {
                    color = Color32.Lerp(exploredColor, visibleColor, vision);
                }
                else if (explored > 0.1f)
                {
                    color = Color32.Lerp(fogColor, exploredColor, explored);
                }
                else
                {
                    color = fogColor;
                }
                
                outputData[i] = color;
            }
        }

        void UpdateSourcesArray()
        {
            if (sourcesArray.Length != visionSources.Count)
            {
                if (sourcesArray.IsCreated)
                    sourcesArray.Dispose();
                
                sourcesArray = new NativeArray<VisionSource>(
                    visionSources.Count, 
                    Allocator.Persistent
                );
            }

            for (int i = 0; i < visionSources.Count; i++)
            {
                sourcesArray[i] = visionSources[i];
            }
        }

        void ApplyToTexture()
        {
            if (outputTexture != null)
            {
                outputTexture.SetPixelData(outputData, 0);
                outputTexture.Apply(false);
            }
        }

        public int RegisterVisionSource(Vector3 worldPosition, float radius)
        {
            // Конвертируем в нормализованные координаты
            float normalizedX = (worldPosition.x + mapSize.x * 0.5f) / mapSize.x;
            float normalizedY = (worldPosition.z + mapSize.y * 0.5f) / mapSize.y;
            
            // Нормализованный радиус
            float normalizedRadius = radius / math.max(mapSize.x, mapSize.y);
            
            VisionSource source = new VisionSource
            {
                position = new float2(normalizedX, normalizedY),
                radius = normalizedRadius
            };

            visionSources.Add(source);
            
            // Для отладки
            debugWorldPositions.Add(worldPosition);
            debugRadii.Add(radius);
            
            Debug.Log($"Registered vision source #{visionSources.Count} at {worldPosition}, " +
                     $"normalized: ({normalizedX:F3}, {normalizedY:F3}), " +
                     $"radius: {radius}, normalized radius: {normalizedRadius:F3}");
            
            return visionSources.Count - 1;
        }

        public void ClearAllSources()
        {
            visionSources.Clear();
            debugWorldPositions.Clear();
            debugRadii.Clear();
        }

        public void UpdateSourcePosition(int index, Vector3 worldPosition)
        {
            if (index < 0 || index >= visionSources.Count)
            {
                Debug.LogWarning($"Invalid source index: {index}");
                return;
            }

            float normalizedX = (worldPosition.x + mapSize.x * 0.5f) / mapSize.x;
            float normalizedY = (worldPosition.z + mapSize.y * 0.5f) / mapSize.y;
            
            VisionSource source = visionSources[index];
            source.position = new float2(normalizedX, normalizedY);
            visionSources[index] = source;
            
            // Обновляем отладочную информацию
            if (index < debugWorldPositions.Count)
            {
                debugWorldPositions[index] = worldPosition;
            }
        }

        public bool IsPositionVisible(Vector3 worldPosition, float threshold = 0.3f)
        {
            // Конвертируем в тексельные координаты
            float normalizedX = (worldPosition.x + mapSize.x * 0.5f) / mapSize.x;
            float normalizedY = (worldPosition.z + mapSize.y * 0.5f) / mapSize.y;
            
            int texX = (int)(normalizedX * textureResolution);
            int texY = (int)(normalizedY * textureResolution);
            
            texX = math.clamp(texX, 0, textureResolution - 1);
            texY = math.clamp(texY, 0, textureResolution - 1);
            
            int index = texY * textureResolution + texX;
            
            if (index >= 0 && index < visionData.Length)
            {
                float visibility = visionData[index];
                return visibility > threshold;
            }
            
            return false;
        }

        void OnDestroy()
        {
            // Завершаем job
            if (jobScheduled)
            {
                currentJobHandle.Complete();
            }

            // Освобождаем Native массивы
            if (visionData.IsCreated) visionData.Dispose();
            if (exploredData.IsCreated) exploredData.Dispose();
            if (outputData.IsCreated) outputData.Dispose();
            if (sourcesArray.IsCreated) sourcesArray.Dispose();

            // Уничтожаем объекты
            if (outputTexture != null) Destroy(outputTexture);
            if (fogMaterial != null) Destroy(fogMaterial);
        }

        void OnDrawGizmosSelected()
        {
            // Границы карты
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position;
            Vector3 size = new Vector3(mapSize.x, 0.1f, mapSize.y);
            Gizmos.DrawWireCube(center, size);

            // Источники видимости
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            for (int i = 0; i < debugWorldPositions.Count; i++)
            {
                if (i < debugRadii.Count)
                {
                    Gizmos.DrawWireSphere(debugWorldPositions[i], debugRadii[i]);
                }
            }
        }
    }

    [BurstCompile]
    public struct DecayJob : IJobParallelFor
    {
        public NativeArray<float> visionData;
        public NativeArray<float> exploredData;
        public float decayRate;

        public void Execute(int index)
        {
            visionData[index] = math.max(0f, visionData[index] - decayRate);
            exploredData[index] = math.max(0f, exploredData[index] - decayRate * 0.1f);
        }
    }

    [BurstCompile]
    public struct UpdateVisionJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<float> visionData;
        
        [NativeDisableParallelForRestriction]
        public NativeArray<float> exploredData;
        
        [ReadOnly] public NativeArray<FogOfWarCPU.VisionSource> sources;
        [ReadOnly] public int resolution;
        [ReadOnly] public float invResolution;

        public void Execute(int y)
        {
            int rowStart = y * resolution;
            float pixelY = (y + 0.5f) * invResolution;
            
            for (int x = 0; x < resolution; x++)
            {
                int index = rowStart + x;
                float pixelX = (x + 0.5f) * invResolution;
                
                float maxVision = visionData[index];
                
                // Проверяем все источники
                for (int i = 0; i < sources.Length; i++)
                {
                    var source = sources[i];
                    
                    float dx = pixelX - source.position.x;
                    float dy = pixelY - source.position.y;
                    float distanceSqr = dx * dx + dy * dy;
                    float radiusSqr = source.radius * source.radius;
                    
                    if (distanceSqr <= radiusSqr)
                    {
                        float distance = math.sqrt(distanceSqr);
                        float normalizedDist = distance / source.radius;
                        float visibility = 1f - normalizedDist;
                        
                        maxVision = math.max(maxVision, visibility);
                        
                        // Обновляем explored
                        if (visibility > 0.1f)
                        {
                            exploredData[index] = math.max(exploredData[index], visibility * 0.3f);
                        }
                    }
                }
                
                visionData[index] = maxVision;
            }
        }
    }

    [BurstCompile]
    public struct CompositeJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float> visionData;
        [ReadOnly] public NativeArray<float> exploredData;
        [WriteOnly] public NativeArray<Color32> outputData;
        
        [ReadOnly] public Color32 fogColor;
        [ReadOnly] public Color32 exploredColor;
        [ReadOnly] public Color32 visibleColor;

        public void Execute(int index)
        {
            float vision = visionData[index];
            float explored = exploredData[index];

            Color32 color;
            
            if (vision > 0.1f)
            {
                color = Color32.Lerp(exploredColor, visibleColor, vision);
            }
            else if (explored > 0.1f)
            {
                color = Color32.Lerp(fogColor, exploredColor, explored);
            }
            else
            {
                color = fogColor;
            }
            
            outputData[index] = color;
        }
    }
}