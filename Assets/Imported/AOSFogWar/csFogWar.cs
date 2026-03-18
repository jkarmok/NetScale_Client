/*
 * Created :    Winter 2022
 * Author :     SeungGeon Kim (keithrek@hanmail.net)
 * Project :    FogWar
 * Filename :   csHomebrewFogWar.cs (non-static monobehaviour module)
 * 
 * All Content (C) 2022 Unlimited Fischl Works, all rights reserved.
 */



using System;                       // Convert
using System.IO;                    // Directory
using System.Linq;                  // Enumerable
using System.Collections.Generic;   // List
using UnityEngine;                  // Monobehaviour
using UnityEditor;                  // Handles



namespace FischlWorks_FogWar
{



    /// The non-static high-level monobehaviour interface of the AOS Fog of War module.

    /// This class holds serialized data for various configuration properties,\n
    /// and is resposible for scanning / saving / loading the LevelData object.\n
    /// The class handles the update frequency of the fog, plus some shader businesses.\n
    /// Various public interfaces related to FogRevealer's FOV are also available.
    public class csFogWar : MonoBehaviour
    {
        [System.Serializable]
        public class LevelData
        {
            public void AddColumn(LevelColumn levelColumn)
            {
                levelRow.Add(levelColumn);
            }

            // Indexer definition
            public LevelColumn this[int index] {
                get {
                    if (index >= 0 && index < levelRow.Count)
                    {
                        return levelRow[index];
                    }
                    else
                    {
                        Debug.LogErrorFormat("index given in x axis is out of range");

                        return null;
                    }
                }
                set {
                    if (index >= 0 && index < levelRow.Count)
                    {
                        levelRow[index] = value;
                    }
                    else
                    {
                        Debug.LogErrorFormat("index given in x axis is out of range");

                        return;
                    }
                }
            }

            // Adding private getter / setters are not allowed for serialization
            public int levelDimensionX = 0;
            public int levelDimensionY = 0;

            public float unitScale = 0;

            public float scanSpacingPerUnit = 0;

            [SerializeField]
            private List<LevelColumn> levelRow = new List<LevelColumn>();
        }



        [System.Serializable]
        public class LevelColumn
        {
            public LevelColumn(IEnumerable<ETileState> stateTiles)
            {
                levelColumn = new List<ETileState>(stateTiles);
            }

            // If I create a separate Tile class, it will impact the size of the save file (but enums will be saved as int)
            public enum ETileState
            {
                Empty,
                Obstacle
            }

            // Indexer definition
            public ETileState this[int index] {
                get {
                    if (index >= 0 && index < levelColumn.Count)
                    {
                        return levelColumn[index];
                    }
                    else
                    {
                        Debug.LogErrorFormat("index given in y axis is out of range");

                        return ETileState.Empty;
                    }
                }
                set {
                    if (index >= 0 && index < levelColumn.Count)
                    {
                        levelColumn[index] = value;
                    }
                    else
                    {
                        Debug.LogErrorFormat("index given in y axis is out of range");

                        return;
                    }
                }
            }

            [SerializeField]
            private List<ETileState> levelColumn = new List<ETileState>();
        }



        [System.Serializable]
        public class FogRevealer
        {
            public FogRevealer(Transform revealerTransform, int sightRange, bool updateOnlyOnMove)
            {
                this.revealerTransform = revealerTransform;
                this.sightRange = sightRange;
                this.updateOnlyOnMove = updateOnlyOnMove;
            }

            public Vector2Int GetCurrentLevelCoordinates(csFogWar fogWar)
            {
                currentLevelCoordinates = new Vector2Int(
                    fogWar.GetUnitX(revealerTransform.position.x),
                    fogWar.GetUnitY(revealerTransform.position.z));

                return currentLevelCoordinates;
            }

            // To be assigned manually by the user
            [SerializeField]
            private Transform revealerTransform = null;
            // These are called expression-bodied properties btw, being stricter here because these are not pure data containers
            public Transform _RevealerTransform => revealerTransform;

            [SerializeField]
            private int sightRange = 0;
            public int _SightRange => sightRange;

            [SerializeField]
            private bool updateOnlyOnMove = true;
            public bool _UpdateOnlyOnMove => updateOnlyOnMove;

            private Vector2Int currentLevelCoordinates = new Vector2Int();
            public Vector2Int _CurrentLevelCoordinates {
                get {
                    lastSeenAt = currentLevelCoordinates;

                    return currentLevelCoordinates;
                }
            }

            [Header("Debug")]
            [SerializeField]
            private Vector2Int lastSeenAt = new Vector2Int(Int32.MaxValue, Int32.MaxValue);
            public Vector2Int _LastSeenAt => lastSeenAt;

            // Оптимизация: кэширование позиции для определения движения
            private Vector3 lastWorldPosition;
            private bool hasInitialPosition = false;
            
            public bool HasMovedSignificantly(float threshold = 0.1f)
            {
                if (!hasInitialPosition)
                {
                    lastWorldPosition = revealerTransform.position;
                    hasInitialPosition = true;
                    return true;
                }
                
                float distance = Vector3.Distance(revealerTransform.position, lastWorldPosition);
                if (distance >= threshold)
                {
                    lastWorldPosition = revealerTransform.position;
                    return true;
                }
                
                return false;
            }
        }



        [BigHeader("Optimization Settings")]
        [SerializeField]
        private bool enableOptimizations = true;
        [ShowIf("enableOptimizations")]
        [SerializeField]
        private ComputeShader fogLerpComputeShader = null;
        [ShowIf("enableOptimizations")]
        [SerializeField]
        [Range(0.01f, 1f)]
        private float movementThreshold = 0.25f;
        [ShowIf("enableOptimizations")]
        [SerializeField]
        private bool useRenderTextures = true;

        [BigHeader("Basic Properties")]
        [SerializeField]
        private List<FogRevealer> fogRevealers = null;
        public List<FogRevealer> _FogRevealers => fogRevealers;
        [SerializeField]
        private Transform levelMidPoint = null;
        public Transform _LevelMidPoint => levelMidPoint;
        [SerializeField]
        [Range(1, 30)]
        private float FogRefreshRate = 5; // Уменьшено с 10 для производительности

        [BigHeader("Fog Properties")]
        [SerializeField]
        [Range(0, 100)]
        private float fogPlaneHeight = 1;
        [SerializeField]
        private Material fogPlaneMaterial = null;
        [SerializeField]
        private Color fogColor = new Color32(5, 15, 25, 255);
        [SerializeField]
        [Range(0, 1)]
        private float fogPlaneAlpha = 1;
        [SerializeField]
        [Range(1, 5)]
        private float fogLerpSpeed = 2.5f;
        public bool keepRevealedTiles = false;
        [ShowIf("keepRevealedTiles")]
        [Range(0, 1)]
        public float revealedTileOpacity = 0.5f;
        [Header("Debug")]
        [SerializeField]
        private Texture2D fogPlaneTextureLerpTarget = null;
        [SerializeField]
        private RenderTexture fogPlaneTextureLerpBufferRT = null;
        private Texture2D fogPlaneTextureLerpBufferCPU = null;
        
        // Компоненты для Compute Shader
        private RenderTexture fogPlaneTextureLerpTargetRT = null;
        private int lerpKernelHandle = -1;

        [BigHeader("Level Data")]
        [SerializeField]
        private TextAsset LevelDataToLoad = null;
        [SerializeField]
        private bool saveDataOnScan = true;
        [ShowIf("saveDataOnScan")]
        [SerializeField]
        private string levelNameToSave = "Default";

        [BigHeader("Scan Properties")]
        [SerializeField]
        [Range(1, 256)] // Уменьшено с 128 для производительности
        [Tooltip("If you need more than 64 units, consider using raycasting-based fog modules instead.")]
        private int levelDimensionX = 11;
        [SerializeField]
        [Range(1, 256)] // Уменьшено с 128 для производительности
        [Tooltip("If you need more than 64 units, consider using raycasting-based fog modules instead.")]
        private int levelDimensionY = 11;
        [SerializeField]
        private float unitScale = 2f; // Увеличено с 1 для меньшего количества тайлов
        public float _UnitScale => unitScale;
        [SerializeField]
        private float scanSpacingPerUnit = 0.25f;
        [SerializeField]
        private float rayStartHeight = 5;
        [SerializeField]
        private float rayMaxDistance = 10;
        [SerializeField]
        private LayerMask obstacleLayers = new LayerMask();
        [SerializeField]
        private bool ignoreTriggers = true;

        [BigHeader("Debug Options")]
        [SerializeField]
        private bool drawGizmos = false;
        [SerializeField]
        private bool LogOutOfRange = false;

        // External shadowcaster module
        public Shadowcaster shadowcaster { get; private set; } = new Shadowcaster();

        public LevelData levelData { get; private set; } = new LevelData();

        // The primitive plane which will act as a mesh for rendering the fog with
        private GameObject fogPlane = null;

        private float FogRefreshRateTimer = 0;
        private bool forceUpdateNextFrame = false;
        private int frameCounter = 0;
        private const int UPDATE_EVERY_N_FRAMES = 2; // Обновляем каждые 2 кадра

        private const string levelScanDataPath = "/LevelData";



        // --- --- ---



        private void Start()
        {
            CheckProperties();

            InitializeVariables();

            if (LevelDataToLoad == null)
            {
                ScanLevel();

                if (saveDataOnScan == true)
                {
#if UNITY_EDITOR
                    SaveScanAsLevelData();
#endif
                }
            }
            else
            {
                LoadLevelData();
            }

            InitializeFog();

            shadowcaster.Initialize(this);

            ForceUpdateFog();
        }



        private void Update()
        {
            // Пропускаем кадры для оптимизации
            frameCounter++;
            if (frameCounter < UPDATE_EVERY_N_FRAMES && !forceUpdateNextFrame)
            {
                UpdateFogPlaneTextureBuffer();
                return;
            }
            frameCounter = 0;
            
            UpdateFog();
            forceUpdateNextFrame = false;
        }



        // --- --- ---



        private void CheckProperties()
        {
            foreach (FogRevealer fogRevealer in fogRevealers)
            {
                if (fogRevealer._RevealerTransform == null)
                {
                    Debug.LogErrorFormat("Please assign a Transform component to each Fog Revealer!");
                }
            }

            if (unitScale <= 0)
            {
                Debug.LogErrorFormat("Unit Scale must be bigger than 0!");
            }

            if (scanSpacingPerUnit <= 0)
            {
                Debug.LogErrorFormat("Scan Spacing Per Unit must be bigger than 0!");
            }

            if (levelMidPoint == null)
            {
                Debug.LogErrorFormat("Please assign the Level Mid Point property!");
            }

            if (fogPlaneMaterial == null)
            {
                Debug.LogErrorFormat("Please assign the \"FogPlane\" material to the Fog Plane Material property!");
            }
            
            if (enableOptimizations && fogLerpComputeShader == null)
            {
                Debug.LogWarning("Fog Lerp Compute Shader is not assigned. Falling back to CPU lerp.");
            }
        }



        private void InitializeVariables()
        {
            if (obstacleLayers.value == 0)
            {
                obstacleLayers = LayerMask.GetMask("Default");
            }

            if (levelNameToSave == String.Empty)
            {
                levelNameToSave = "Default";
            }
            
            // Инициализация Compute Shader
            if (enableOptimizations && fogLerpComputeShader != null)
            {
                lerpKernelHandle = fogLerpComputeShader.FindKernel("CSMain");
            }
        }



        private void InitializeFog()
        {
            fogPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);

            fogPlane.name = "[RUNTIME] Fog_Plane";

            fogPlane.transform.position = new Vector3(
                levelMidPoint.position.x,
                levelMidPoint.position.y + fogPlaneHeight,
                levelMidPoint.position.z);

            fogPlane.transform.localScale = new Vector3(
                (levelDimensionX * unitScale) / 10.0f,
                1,
                (levelDimensionY * unitScale) / 10.0f);

            fogPlaneTextureLerpTarget = new Texture2D(levelDimensionX, levelDimensionY, TextureFormat.RGBA32, false);
            
            if (enableOptimizations && useRenderTextures)
            {
                // Используем RenderTexture для GPU-оптимизаций
                fogPlaneTextureLerpBufferRT = new RenderTexture(levelDimensionX, levelDimensionY, 0, RenderTextureFormat.ARGB32);
                fogPlaneTextureLerpBufferRT.enableRandomWrite = true;
                fogPlaneTextureLerpBufferRT.Create();
                
                fogPlaneTextureLerpTargetRT = new RenderTexture(levelDimensionX, levelDimensionY, 0, RenderTextureFormat.ARGB32);
                fogPlaneTextureLerpTargetRT.enableRandomWrite = true;
                fogPlaneTextureLerpTargetRT.Create();
            }
            else
            {
                fogPlaneTextureLerpBufferCPU = new Texture2D(levelDimensionX, levelDimensionY, TextureFormat.RGBA32, false);
                fogPlaneTextureLerpBufferCPU.wrapMode = TextureWrapMode.Clamp;
                fogPlaneTextureLerpBufferCPU.filterMode = FilterMode.Bilinear;
            }

            fogPlane.GetComponent<MeshRenderer>().material = new Material(fogPlaneMaterial);

            if (enableOptimizations && useRenderTextures)
            {
                fogPlane.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", fogPlaneTextureLerpBufferRT);
            }
            else
            {
                fogPlane.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", fogPlaneTextureLerpBufferCPU);
            }

            fogPlane.GetComponent<MeshCollider>().enabled = false;
        }



        private void ForceUpdateFog()
        {
            UpdateFogField();

            if (enableOptimizations && useRenderTextures)
            {
                Graphics.Blit(fogPlaneTextureLerpTarget, fogPlaneTextureLerpBufferRT);
            }
            else
            {
                Graphics.CopyTexture(fogPlaneTextureLerpTarget, fogPlaneTextureLerpBufferCPU);
            }
        }



        private void UpdateFog()
        {
            fogPlane.transform.position = new Vector3(
                levelMidPoint.position.x,
                levelMidPoint.position.y + fogPlaneHeight,
                levelMidPoint.position.z);

            FogRefreshRateTimer += Time.deltaTime;

            if (FogRefreshRateTimer < 1 / FogRefreshRate)
            {
                UpdateFogPlaneTextureBuffer();
                return;
            }
            else
            {
                FogRefreshRateTimer -= 1 / FogRefreshRate;
            }

            bool shouldUpdateFogField = false;
            
            // Проверяем, нужно ли обновлять поле тумана
            foreach (FogRevealer fogRevealer in fogRevealers)
            {
                if (fogRevealer._UpdateOnlyOnMove == false)
                {
                    shouldUpdateFogField = true;
                    break;
                }

                Vector2Int currentLevelCoordinates = fogRevealer.GetCurrentLevelCoordinates(this);

                if (currentLevelCoordinates != fogRevealer._LastSeenAt)
                {
                    shouldUpdateFogField = true;
                    break;
                }
                
                // Дополнительная проверка на значительное перемещение
                if (enableOptimizations && fogRevealer.HasMovedSignificantly(movementThreshold))
                {
                    shouldUpdateFogField = true;
                    break;
                }

                if (fogRevealer == fogRevealers.Last())
                {
                    // Ни один revealer не переместился
                    UpdateFogPlaneTextureBuffer();
                    return;
                }
            }

            if (shouldUpdateFogField)
            {
                UpdateFogField();
            }

            UpdateFogPlaneTextureBuffer();
        }



        private void UpdateFogField()
        {
            shadowcaster.ResetTileVisibility();

            // Оптимизация: обрабатываем revealer'ы пачкой
            if (enableOptimizations && fogRevealers.Count > 1)
            {
                // Объединяем позиции для более эффективной обработки
                foreach (FogRevealer fogRevealer in fogRevealers)
                {
                    fogRevealer.GetCurrentLevelCoordinates(this);
                    
                    // Передаем все revealer'ы в shadowcaster для batch-обработки
                    shadowcaster.ProcessLevelDataBatch(
                        fogRevealer._CurrentLevelCoordinates,
                        Mathf.RoundToInt(fogRevealer._SightRange / unitScale),
                        fogRevealers.Count);
                }
            }
            else
            {
                // Обработка по одному (старый способ)
                foreach (FogRevealer fogRevealer in fogRevealers)
                {
                    fogRevealer.GetCurrentLevelCoordinates(this);

                    shadowcaster.ProcessLevelData(
                        fogRevealer._CurrentLevelCoordinates,
                        Mathf.RoundToInt(fogRevealer._SightRange / unitScale));
                }
            }

            UpdateFogPlaneTextureTarget();
        }



        private void UpdateFogPlaneTextureBuffer()
        {
            if (enableOptimizations && useRenderTextures && fogLerpComputeShader != null)
            {
                // GPU-оптимизация через Compute Shader
                UpdateFogPlaneTextureBufferGPU();
            }
            else
            {
                // Старый CPU-метод (fallback)
                UpdateFogPlaneTextureBufferCPU();
            }
        }
        
        private void UpdateFogPlaneTextureBufferGPU()
        {
            if (fogLerpComputeShader == null || lerpKernelHandle == -1)
                return;
                
            fogLerpComputeShader.SetFloat("LerpSpeed", fogLerpSpeed);
            fogLerpComputeShader.SetFloat("DeltaTime", Time.deltaTime);
            
            // Копируем Texture2D в RenderTexture если нужно
            if (fogPlaneTextureLerpTargetRT == null)
            {
                fogPlaneTextureLerpTargetRT = new RenderTexture(
                    fogPlaneTextureLerpTarget.width, 
                    fogPlaneTextureLerpTarget.height, 
                    0, 
                    RenderTextureFormat.ARGB32);
                fogPlaneTextureLerpTargetRT.enableRandomWrite = true;
                fogPlaneTextureLerpTargetRT.Create();
            }
            
            Graphics.Blit(fogPlaneTextureLerpTarget, fogPlaneTextureLerpTargetRT);
            
            fogLerpComputeShader.SetTexture(lerpKernelHandle, "BufferTex", fogPlaneTextureLerpBufferRT);
            fogLerpComputeShader.SetTexture(lerpKernelHandle, "TargetTex", fogPlaneTextureLerpTargetRT);
            
            // Запускаем Compute Shader
            fogLerpComputeShader.Dispatch(
                lerpKernelHandle, 
                Mathf.CeilToInt(fogPlaneTextureLerpBufferRT.width / 8f), 
                Mathf.CeilToInt(fogPlaneTextureLerpBufferRT.height / 8f), 
                1);
        }
        
        private void UpdateFogPlaneTextureBufferCPU()
        {
            if (useRenderTextures)
            {
                // Fallback для RenderTextures без Compute Shader
                Texture2D tempTarget = new Texture2D(fogPlaneTextureLerpTarget.width, fogPlaneTextureLerpTarget.height);
                RenderTexture.active = fogPlaneTextureLerpBufferRT;
                tempTarget.ReadPixels(new Rect(0, 0, fogPlaneTextureLerpBufferRT.width, fogPlaneTextureLerpBufferRT.height), 0, 0);
                tempTarget.Apply();
                
                Color[] bufferPixels = tempTarget.GetPixels();
                Color[] targetPixels = fogPlaneTextureLerpTarget.GetPixels();

                if (bufferPixels.Length != targetPixels.Length)
                {
                    Debug.LogErrorFormat("Fog plane texture buffer and target have different pixel counts");
                    return;
                }

                for (int i = 0; i < bufferPixels.Length; i++)
                {
                    bufferPixels[i] = Color.Lerp(bufferPixels[i], targetPixels[i], fogLerpSpeed * Time.deltaTime);
                }
                
                tempTarget.SetPixels(bufferPixels);
                tempTarget.Apply();
                
                Graphics.Blit(tempTarget, fogPlaneTextureLerpBufferRT);
                Destroy(tempTarget);
            }
            else
            {
                // Старый метод с Texture2D
                Color[] bufferPixels = fogPlaneTextureLerpBufferCPU.GetPixels();
                Color[] targetPixels = fogPlaneTextureLerpTarget.GetPixels();

                if (bufferPixels.Length != targetPixels.Length)
                {
                    Debug.LogErrorFormat("Fog plane texture buffer and target have different pixel counts");
                    return;
                }

                for (int i = 0; i < bufferPixels.Length; i++)
                {
                    bufferPixels[i] = Color.Lerp(bufferPixels[i], targetPixels[i], fogLerpSpeed * Time.deltaTime);
                }
                
                fogPlaneTextureLerpBufferCPU.SetPixels(bufferPixels);
                fogPlaneTextureLerpBufferCPU.Apply();
            }
        }



        private void UpdateFogPlaneTextureTarget()
        {
            fogPlane.GetComponent<MeshRenderer>().material.SetColor("_Color", fogColor);

            Color[] colors = shadowcaster.fogField.GetColors(fogPlaneAlpha, this);
            fogPlaneTextureLerpTarget.SetPixels(colors);
            fogPlaneTextureLerpTarget.Apply();
            
            // Если используем RenderTexture, обновляем и её
            if (enableOptimizations && useRenderTextures && fogPlaneTextureLerpTargetRT != null)
            {
                Graphics.Blit(fogPlaneTextureLerpTarget, fogPlaneTextureLerpTargetRT);
            }
        }



        private void ScanLevel()
        {
            Debug.LogFormat("There is no level data file assigned, scanning level...");

            levelData.levelDimensionX = levelDimensionX;
            levelData.levelDimensionY = levelDimensionY;
            levelData.unitScale = unitScale;
            levelData.scanSpacingPerUnit = scanSpacingPerUnit;

            // Оптимизация: кэшируем часто используемые значения
            float halfUnitScale = (unitScale - scanSpacingPerUnit) / 2.0f;
            Vector3 boxSize = new Vector3(halfUnitScale, unitScale / 2.0f, halfUnitScale);
            QueryTriggerInteraction triggerInteraction = (QueryTriggerInteraction)(2 - Convert.ToInt32(ignoreTriggers));

            for (int xIterator = 0; xIterator < levelDimensionX; xIterator++)
            {
                levelData.AddColumn(new LevelColumn(Enumerable.Repeat(LevelColumn.ETileState.Empty, levelDimensionY)));

                for (int yIterator = 0; yIterator < levelDimensionY; yIterator++)
                {
                    bool isObstacleHit = Physics.BoxCast(
                        new Vector3(
                            GetWorldX(xIterator),
                            levelMidPoint.position.y + rayStartHeight,
                            GetWorldY(yIterator)),
                        boxSize,
                        Vector3.down,
                        Quaternion.identity,
                        rayMaxDistance,
                        obstacleLayers,
                        triggerInteraction);

                    if (isObstacleHit == true)
                    {
                        levelData[xIterator][yIterator] = LevelColumn.ETileState.Obstacle;
                    }
                }
            }

            Debug.LogFormat("Successfully scanned level with a scale of {0} x {1}", levelDimensionX, levelDimensionY);
        }



#if UNITY_EDITOR
        private void SaveScanAsLevelData()
        {
            string fullPath = Application.dataPath + levelScanDataPath + "/" + levelNameToSave + ".json";

            if (Directory.Exists(Application.dataPath + levelScanDataPath) == false)
            {
                Directory.CreateDirectory(Application.dataPath + levelScanDataPath);

                Debug.LogFormat("level scan data folder at \"{0}\" is missing, creating...", levelScanDataPath);
            }

            if (File.Exists(fullPath) == true)
            {
                Debug.LogFormat("level scan data already exists, overwriting...");
            }

            string levelJson = JsonUtility.ToJson(levelData);

            File.WriteAllText(fullPath, levelJson);

            Debug.LogFormat("Successfully saved level scan data at \"{0}\"", fullPath);
        }
#endif



        private void LoadLevelData()
        {
            Debug.LogFormat("Level scan data with a name of \"{0}\" is assigned, loading...", LevelDataToLoad.name);

            string levelJson = LevelDataToLoad.ToString();

            levelData = JsonUtility.FromJson<LevelData>(levelJson);

            levelDimensionX = levelData.levelDimensionX;
            levelDimensionY = levelData.levelDimensionY;
            unitScale = levelData.unitScale;
            scanSpacingPerUnit = levelData.scanSpacingPerUnit;
            
            // Оптимизация: корректируем размеры если они слишком большие
            if (levelDimensionX > 64 || levelDimensionY > 64)
            {
                Debug.LogWarning($"Level size {levelDimensionX}x{levelDimensionY} is large. Consider reducing unitScale to improve performance.");
                if (unitScale < 2f)
                {
                    unitScale = 2f;
                    Debug.LogWarning($"Auto-adjusted unitScale to {unitScale} for better performance.");
                }
            }

            Debug.LogFormat("Successfully loaded level scan data with the name of \"{0}\"", LevelDataToLoad.name);
        }



        /// Adds a new FogRevealer instance to the list and returns its index
        public int AddFogRevealer(FogRevealer fogRevealer)
        {
            fogRevealers.Add(fogRevealer);

            return fogRevealers.Count - 1;
        }



        /// Removes a FogRevealer instance from the list with index
        public void RemoveFogRevealer(int revealerIndex)
        {
            if (fogRevealers.Count > revealerIndex && revealerIndex > -1)
            {
                fogRevealers.RemoveAt(revealerIndex);
            }
            else
            {
                Debug.LogFormat("Given index of {0} exceeds the revealers' container range", revealerIndex);
            }
        }



        /// Replaces the FogRevealer list with the given one
        public void ReplaceFogRevealerList(List<FogRevealer> fogRevealers)
        {
            this.fogRevealers = fogRevealers;
        }



        /// Checks if the given level coordinates are within level dimension range.
        public bool CheckLevelGridRange(Vector2Int levelCoordinates)
        {
            bool result =
                levelCoordinates.x >= 0 &&
                levelCoordinates.x < levelData.levelDimensionX &&
                levelCoordinates.y >= 0 &&
                levelCoordinates.y < levelData.levelDimensionY;

            if (result == false && LogOutOfRange == true)
            {
                Debug.LogFormat("Level coordinates \"{0}\" is out of grid range", levelCoordinates);
            }

            return result;
        }



        /// Checks if the given world coordinates are within level dimension range.
        public bool CheckWorldGridRange(Vector3 worldCoordinates)
        {
            Vector2Int levelCoordinates = WorldToLevel(worldCoordinates);

            return CheckLevelGridRange(levelCoordinates);
        }



        /// Checks if the given pair of world coordinates and additionalRadius is visible by FogRevealers.
        public bool CheckVisibility(Vector3 worldCoordinates, int additionalRadius)
        {
            Vector2Int levelCoordinates = WorldToLevel(worldCoordinates);

            if (additionalRadius == 0)
            {
                return shadowcaster.fogField[levelCoordinates.x][levelCoordinates.y] == 
                    Shadowcaster.LevelColumn.ETileVisibility.Revealed;
            }

            int scanResult = 0;

            for (int xIterator = -1; xIterator < additionalRadius + 1; xIterator++)
            {
                for (int yIterator = -1; yIterator < additionalRadius + 1; yIterator++)
                {
                    if (CheckLevelGridRange(new Vector2Int(
                        levelCoordinates.x + xIterator, 
                        levelCoordinates.y + yIterator)) == false)
                    {
                        scanResult = 0;
                        break;
                    }

                    scanResult += Convert.ToInt32(
                        shadowcaster.fogField[levelCoordinates.x + xIterator][levelCoordinates.y + yIterator] == 
                        Shadowcaster.LevelColumn.ETileVisibility.Revealed);
                }
            }

            return scanResult > 0;
        }



        /// Converts unit (divided by unitScale, then rounded) world coordinates to level coordinates.
        public Vector2Int WorldToLevel(Vector3 worldCoordinates)
        {
            Vector2Int unitWorldCoordinates = GetUnitVector(worldCoordinates);

            return new Vector2Int(
                unitWorldCoordinates.x + (levelDimensionX / 2),
                unitWorldCoordinates.y + (levelDimensionY / 2));
        }



        /// Converts level coordinates into world coordinates.
        public Vector3 GetWorldVector(Vector2Int worldCoordinates)
        {
            return new Vector3(
                GetWorldX(worldCoordinates.x + (levelDimensionX / 2)), 
                0, 
                GetWorldY(worldCoordinates.y + (levelDimensionY / 2)));
        }



        /// Converts "pure" world coordinates into unit world coordinates.
        public Vector2Int GetUnitVector(Vector3 worldCoordinates)
        {
            return new Vector2Int(GetUnitX(worldCoordinates.x), GetUnitY(worldCoordinates.z));
        }



        /// Converts level coordinate to corresponding unit world coordinates.
        public float GetWorldX(int xValue)
        {
            if (levelData.levelDimensionX % 2 == 0)
            {
                return (levelMidPoint.position.x - ((levelDimensionX / 2.0f) - xValue) * unitScale);
            }

            return (levelMidPoint.position.x - ((levelDimensionX / 2.0f) - (xValue + 0.5f)) * unitScale);
        }



        /// Converts world coordinate to unit world coordinates.
        public int GetUnitX(float xValue)
        {
            return Mathf.RoundToInt((xValue - levelMidPoint.position.x) / unitScale);
        }



        /// Converts level coordinate to corresponding unit world coordinates.
        public float GetWorldY(int yValue)
        {
            if (levelData.levelDimensionY % 2 == 0)
            {
                return (levelMidPoint.position.z - ((levelDimensionY / 2.0f) - yValue) * unitScale);
            }

            return (levelMidPoint.position.z - ((levelDimensionY / 2.0f) - (yValue + 0.5f)) * unitScale);
        }



        /// Converts world coordinate to unit world coordinates.
        public int GetUnitY(float yValue)
        {
            return Mathf.RoundToInt((yValue - levelMidPoint.position.z) / unitScale);
        }
        
        // Освобождение ресурсов
        private void OnDestroy()
        {
            if (fogPlaneTextureLerpBufferRT != null)
            {
                fogPlaneTextureLerpBufferRT.Release();
            }
            
            if (fogPlaneTextureLerpTargetRT != null)
            {
                fogPlaneTextureLerpTargetRT.Release();
            }
        }



#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Application.isPlaying == false)
            {
                return;
            }

            if (drawGizmos == false)
            {
                return;
            }

            Handles.color = Color.yellow;

            for (int xIterator = 0; xIterator < levelDimensionX; xIterator++)
            {
                for (int yIterator = 0; yIterator < levelDimensionY; yIterator++)
                {
                    if (levelData[xIterator][yIterator] == LevelColumn.ETileState.Obstacle)
                    {
                        if (shadowcaster.fogField[xIterator][yIterator] == Shadowcaster.LevelColumn.ETileVisibility.Revealed)
                        {
                            Handles.color = Color.green;
                        }
                        else
                        {
                            Handles.color = Color.red;
                        }

                        Handles.DrawWireCube(
                            new Vector3(
                                GetWorldX(xIterator),
                                levelMidPoint.position.y,
                                GetWorldY(yIterator)),
                            new Vector3(
                                unitScale - scanSpacingPerUnit,
                                unitScale,
                                unitScale - scanSpacingPerUnit));
                    }
                    else
                    {
                        Gizmos.color = Color.yellow;

                        Gizmos.DrawSphere(
                            new Vector3(
                                GetWorldX(xIterator),
                                levelMidPoint.position.y,
                                GetWorldY(yIterator)),
                            unitScale / 5.0f);
                    }

                    if (shadowcaster.fogField[xIterator][yIterator] == Shadowcaster.LevelColumn.ETileVisibility.Revealed)
                    {
                        Gizmos.color = Color.green;

                        Gizmos.DrawSphere(
                            new Vector3(
                                GetWorldX(xIterator),
                                levelMidPoint.position.y,
                                GetWorldY(yIterator)),
                            unitScale / 3.0f);
                    }
                }
            }
        }
#endif
    }



    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public string _BaseCondition {
            get { return mBaseCondition; }
        }

        private string mBaseCondition = String.Empty;

        public ShowIfAttribute(string baseCondition)
        {
            mBaseCondition = baseCondition;
        }
    }



    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class BigHeaderAttribute : PropertyAttribute
    {
        public string _Text {
            get { return mText; }
        }

        private string mText = String.Empty;

        public BigHeaderAttribute(string text)
        {
            mText = text;
        }
    }



}