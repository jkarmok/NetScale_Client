using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Imported.AVIS.RuntimePerformanceMonitor.Providers
{
    public class UIToolkitPerformanceProvider : PerformanceDataProvider
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string containerName = "performance-monitor-container";
        [SerializeField] private bool startMinimized = false;
        [SerializeField] private int graphSampleCount = 100;
        [SerializeField] private float uiUpdateInterval = 0.1f;
        [SerializeField] private float colorTransitionSpeed = 5f;
        [SerializeField] private bool showExtendedStats = true;
        [SerializeField] private Corner corner = Corner.TopRight;
        [SerializeField] private float overlayOpacity = 0.85f;
        [SerializeField] private float backgroundOpacity = 0.7f;
    
        private VisualElement mainContainer;
        private VisualElement contentContainer;
        private Button toggleButton;
    
        // Value labels
        private Label fpsValueLabel;
        private Label frameTimeValueLabel;
        private Label allocValueLabel;
        private Label totalMemoryValueLabel;
        
        // Current value labels in graph headers
        private Label currentFpsLabel;
        private Label currentFrameTimeLabel;
        private Label currentAllocLabel;
        
        // Stats labels - компактные версии
        private Label fpsStatsLabel;
        private Label frameTimeStatsLabel;
        private Label allocStatsLabel;
    
        private VisualElement frameTimeGraph;
        private VisualElement fpsGraph;
        private VisualElement allocGraph;

        private const float GRAPH_HEIGHT = 80f;
        private const float COMPACT_GRAPH_HEIGHT = 60f;
        private const float MAX_FRAME_TIME = 33.33f;
        private const float MAX_FPS = 120f;
        private const float MAX_ALLOC_MB = 1f;
        private const float INV_MAX_FRAME_TIME = 1f / MAX_FRAME_TIME;
        private const float INV_MAX_FPS = 1f / MAX_FPS;
        private const float INV_MAX_ALLOC_MB = 1f / MAX_ALLOC_MB;

        // Graphy-like color palette
        private static readonly Color COLOR_GREEN = new Color(0.298f, 0.686f, 0.314f, 1f);
        private static readonly Color COLOR_YELLOW = new Color(1f, 0.757f, 0.027f, 1f);
        private static readonly Color COLOR_RED = new Color(0.957f, 0.263f, 0.212f, 1f);
        private static readonly Color COLOR_CYAN = new Color(0.129f, 0.588f, 0.953f, 1f);
        private static readonly Color COLOR_WHITE = new Color(1f, 1f, 1f, 1f);
        private static readonly Color COLOR_GRAY = new Color(0.667f, 0.667f, 0.667f, 0.8f);
        private static readonly Color COLOR_GRAPH_FRAME = new Color(0.129f, 0.588f, 0.953f, 0.8f);
        private static readonly Color COLOR_GRAPH_ALLOC = new Color(1f, 0.596f, 0f, 0.8f);
        private static readonly Color COLOR_PURPLE = new Color(0.612f, 0.153f, 0.69f, 1f);
        private static readonly Color BG_MAIN = new Color(0.05f, 0.05f, 0.05f, 0.7f);

        private readonly StringBuilder sb = new StringBuilder(32);
        private bool isExpanded = false;

        private float timeSinceLastUpdate = 0f;
        private bool needsGraphRepaint = false;

        private float[] cachedFrameTimeSamples;
        private float[] cachedFpsSamples;
        private float[] cachedAllocSamples;
        private int cachedCurrentIndex;

        private Color currentFpsColor;
        private Color targetFpsColor;
        private string _deviceInfo = null;

        public enum Corner
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        protected override void Awake()
        {
            base.Awake();
        
            if (uiDocument == null)
                uiDocument = GetComponent<UIDocument>();
            
            if (uiDocument != null)
            {
                InitializeUI();
            
                if (!startMinimized)
                {
                    TogglePanel();
                }
            }
            else
            {
                Debug.LogError("UIToolkitPerformanceProvider: UIDocument not found!");
                enabled = false;
            }
        }

        private void Update()
        {
            if (isExpanded && currentFpsColor != targetFpsColor)
            {
                currentFpsColor = Color.Lerp(currentFpsColor, targetFpsColor, Time.deltaTime * colorTransitionSpeed);
                fpsValueLabel.style.color = currentFpsColor;
            
                if (ColorDistance(currentFpsColor, targetFpsColor) < 0.01f)
                {
                    currentFpsColor = targetFpsColor;
                    fpsValueLabel.style.color = targetFpsColor;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float ColorDistance(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }

        private void InitializeUI()
        {
            var root = uiDocument.rootVisualElement;
            var targetContainer = string.IsNullOrEmpty(containerName) ? root : root.Q<VisualElement>(containerName);
        
            if (targetContainer == null)
                targetContainer = root;
        
            CreateMainContainer();
            CreateToggleButton();
            CreateContentContainer();
            CreateMetricsUI();
        
            targetContainer.Add(mainContainer);
        
            currentFpsColor = COLOR_GREEN;
            targetFpsColor = COLOR_GREEN;
        }

        private void CreateMainContainer()
        {
            mainContainer = new VisualElement
            {
                name = "performance-monitor-overlay",
                pickingMode = PickingMode.Ignore
            };
        
            var style = mainContainer.style;
            style.position = Position.Absolute;
            style.width = 320;
            style.maxWidth = 320;
            style.opacity = overlayOpacity;
            
            // Position based on selected corner
            switch (corner)
            {
                case Corner.TopLeft:
                    style.top = 10;
                    style.left = 10;
                    break;
                case Corner.TopRight:
                    style.top = 10;
                    style.right = 10;
                    break;
                case Corner.BottomLeft:
                    style.bottom = 10;
                    style.left = 10;
                    break;
                case Corner.BottomRight:
                    style.bottom = 10;
                    style.right = 10;
                    break;
            }
            
            style.backgroundColor = Color.clear;
            style.borderBottomLeftRadius = 8;
            style.borderBottomRightRadius = 8;
            style.borderTopLeftRadius = 8;
            style.borderTopRightRadius = 8;
        }

        private void CreateToggleButton()
        {
            toggleButton = new Button(TogglePanel)
            {
                text = "📊"
            };
        
            var style = toggleButton.style;
            style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, backgroundOpacity);
            style.color = COLOR_WHITE;
            style.fontSize = 14;
            style.unityFontStyleAndWeight = FontStyle.Bold;
            style.unityTextAlign = TextAnchor.MiddleCenter;
            style.width = 40;
            style.height = 30;
            style.borderBottomLeftRadius = 8;
            style.borderBottomRightRadius = 8;
            style.borderTopLeftRadius = 8;
            style.borderTopRightRadius = 8;
            style.borderBottomWidth = 1;
            style.borderTopWidth = 1;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            style.borderTopColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            style.borderRightColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        
            toggleButton.RegisterCallback<MouseEnterEvent>(evt => {
                toggleButton.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, backgroundOpacity);
            });
            toggleButton.RegisterCallback<MouseLeaveEvent>(evt => {
                toggleButton.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, backgroundOpacity);
            });
        
            mainContainer.Add(toggleButton);
        }

        private void CreateContentContainer()
        {
            contentContainer = new VisualElement();
            var style = contentContainer.style;
            style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, backgroundOpacity);
            style.borderBottomLeftRadius = 8;
            style.borderBottomRightRadius = 8;
            style.borderTopLeftRadius = 8;
            style.borderTopRightRadius = 8;
            style.borderBottomWidth = 1;
            style.borderTopWidth = 1;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            style.borderTopColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            style.borderRightColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            style.paddingBottom = 8;
            style.paddingTop = 8;
            style.paddingLeft = 8;
            style.paddingRight = 8;
            style.marginTop = 5;
            mainContainer.Add(contentContainer);
        }

        private void CreateMetricsUI()
        {
            // Main metrics row - compact like Graphy
            var mainMetricsRow = new VisualElement();
            mainMetricsRow.style.flexDirection = FlexDirection.Row;
            mainMetricsRow.style.justifyContent = Justify.SpaceBetween;
            mainMetricsRow.style.marginBottom = 8;
        
            CreateCompactMetric(mainMetricsRow, "FPS", out fpsValueLabel, COLOR_GREEN);
            CreateCompactMetric(mainMetricsRow, "MS", out frameTimeValueLabel, COLOR_CYAN);
            CreateCompactMetric(mainMetricsRow, "MEM", out allocValueLabel, COLOR_YELLOW);
            CreateCompactMetric(mainMetricsRow, "TOTAL", out totalMemoryValueLabel, COLOR_PURPLE);
            
            contentContainer.Add(mainMetricsRow);

            // Compact graphs with stats
            var graphsContainer = new VisualElement();
            
            var fpsContainer = CreateCompactGraphSection("FPS", COLOR_GREEN, out fpsGraph, out currentFpsLabel, out fpsStatsLabel);
            graphsContainer.Add(fpsContainer);
            
            var frameTimeContainer = CreateCompactGraphSection("FRAME TIME", COLOR_GRAPH_FRAME, out frameTimeGraph, out currentFrameTimeLabel, out frameTimeStatsLabel);
            graphsContainer.Add(frameTimeContainer);
            
            var allocContainer = CreateCompactGraphSection("ALLOC", COLOR_GRAPH_ALLOC, out allocGraph, out currentAllocLabel, out allocStatsLabel);
            graphsContainer.Add(allocContainer);
            
            contentContainer.Add(graphsContainer);
        }

        private void CreateCompactMetric(VisualElement parent, string label, out Label valueLabel, Color color)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.alignItems = Align.Center;
            container.style.flexGrow = 1;
            
            var labelText = new Label(label);
            labelText.style.fontSize = 9;
            labelText.style.color = COLOR_GRAY;
            labelText.style.unityTextAlign = TextAnchor.MiddleCenter;
            labelText.style.marginBottom = 2;
            container.Add(labelText);
            
            valueLabel = new Label("--");
            valueLabel.style.fontSize = 12;
            valueLabel.style.color = color;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            valueLabel.style.width = 50;
            container.Add(valueLabel);
            
            parent.Add(container);
        }

        private VisualElement CreateCompactGraphSection(string title, Color graphColor, out VisualElement graph, out Label currentValueLabel, out Label statsLabel)
        {
            var container = new VisualElement();
            container.style.marginBottom = 6;
        
            // Header with title and current value
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 2;
            
            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 9;
            titleLabel.style.color = COLOR_GRAY;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(titleLabel);
            
            currentValueLabel = new Label("--");
            currentValueLabel.style.fontSize = 9;
            currentValueLabel.style.color = graphColor;
            currentValueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(currentValueLabel);
            
            container.Add(header);

            // Stats row (min/max/avg)
            var statsRow = new VisualElement();
            statsRow.style.flexDirection = FlexDirection.Row;
            statsRow.style.justifyContent = Justify.SpaceBetween;
            statsRow.style.marginBottom = 3;
            
            statsLabel = new Label("min:0 max:0 avg:0");
            statsLabel.style.fontSize = 8;
            statsLabel.style.color = COLOR_GRAY;
            statsLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            statsRow.Add(statsLabel);
            
            container.Add(statsRow);
        
            // Graph container
            var graphContainer = new VisualElement();
            graphContainer.style.height = COMPACT_GRAPH_HEIGHT;
            graphContainer.style.backgroundColor = new Color(0f, 0f, 0f, 0.3f);
            graphContainer.style.borderBottomLeftRadius = 4;
            graphContainer.style.borderBottomRightRadius = 4;
            graphContainer.style.borderTopLeftRadius = 4;
            graphContainer.style.borderTopRightRadius = 4;
            graphContainer.style.position = Position.Relative;
            
            graph = new VisualElement();
            graph.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            graph.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            graph.generateVisualContent += OnGenerateVisualContent;
            graphContainer.Add(graph);
            
            container.Add(graphContainer);
        
            return container;
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            var graph = ctx.visualElement;
        
            if (graph == frameTimeGraph)
            {
                DrawCompactGraph(ctx, cachedFrameTimeSamples, INV_MAX_FRAME_TIME, COLOR_GRAPH_FRAME);
            }
            else if (graph == fpsGraph)
            {
                DrawCompactGraph(ctx, cachedFpsSamples, INV_MAX_FPS, COLOR_GREEN);
            }
            else if (graph == allocGraph)
            {
                DrawCompactGraph(ctx, cachedAllocSamples, INV_MAX_ALLOC_MB, COLOR_GRAPH_ALLOC);
            }
        }

        private void DrawCompactGraph(MeshGenerationContext ctx, float[] samples, float invMaxValue, Color color)
        {
            if (samples == null) return;
        
            var painter = ctx.painter2D;
            var rect = ctx.visualElement.contentRect;
        
            float w = rect.width;
            float h = rect.height;
        
            if (w < 2f || h < 2f) return;
        
            // Background fill with gradient
            painter.fillColor = new Color(color.r, color.g, color.b, 0.2f);
            painter.BeginPath();
        
            float stepX = w / (graphSampleCount - 1);
            int idx = cachedCurrentIndex;
            int count = graphSampleCount;
        
            painter.MoveTo(new Vector2(0, h));
        
            for (int i = 0; i < count; i++)
            {
                int sampleIndex = (idx + i) % count;
                float value = Mathf.Clamp01(samples[sampleIndex] * invMaxValue);
                float x = i * stepX;
                float y = h - (value * h);
                painter.LineTo(new Vector2(x, y));
            }
        
            painter.LineTo(new Vector2(w, h));
            painter.ClosePath();
            painter.Fill();
        
            // Main line
            painter.strokeColor = color;
            painter.lineWidth = 1.5f;
            painter.BeginPath();
        
            for (int i = 0; i < count; i++)
            {
                int sampleIndex = (idx + i) % count;
                float value = Mathf.Clamp01(samples[sampleIndex] * invMaxValue);
                float x = i * stepX;
                float y = h - (value * h);
            
                if (i == 0)
                    painter.MoveTo(new Vector2(x, y));
                else
                    painter.LineTo(new Vector2(x, y));
            }
        
            painter.Stroke();
        }

        public override void OnPerformanceDataUpdated(ref PerformanceData data)
        {
            cachedFrameTimeSamples = data.frameTimeHistory;
            cachedFpsSamples = data.frameTimeHistory;
            cachedAllocSamples = data.allocHistory;
            cachedCurrentIndex = data.currentIndex;
        
            timeSinceLastUpdate += Time.deltaTime;
            if (timeSinceLastUpdate >= uiUpdateInterval)
            {
                timeSinceLastUpdate -= uiUpdateInterval;
                UpdateUIOptimized(ref data);
                needsGraphRepaint = true;
            }
        
            if (needsGraphRepaint && isExpanded)
            {
                frameTimeGraph.MarkDirtyRepaint();
                fpsGraph.MarkDirtyRepaint();
                allocGraph.MarkDirtyRepaint();
                needsGraphRepaint = false;
            }
        }

        public override void SetupDeviceData(string deviceInfoText)
        {
            _deviceInfo = deviceInfoText;
        }

        private void UpdateUIOptimized(ref PerformanceData data)
        {
            if (!isExpanded) return;
            
            var stats = data.stats;
            int fps = Mathf.RoundToInt(data.fps);
        
            // FPS
            fpsValueLabel.text = fps.ToString();
            targetFpsColor = fps >= 55 ? COLOR_GREEN : (fps >= 30 ? COLOR_YELLOW : COLOR_RED);
            
            // Frame Time
            sb.Clear();
            AppendFloat(sb, data.frameTime, 1);
            frameTimeValueLabel.text = sb.ToString();
            
            // Alloc
            bool isMB = data.allocPerFrameKB >= 1024f;
            float allocValue = isMB ? data.allocPerFrameMB : data.allocPerFrameKB;
            sb.Clear();
            AppendFloat(sb, allocValue, 1);
            sb.Append(isMB ? "M" : "K");
            allocValueLabel.text = sb.ToString();
            
            // Total Memory
            float totalMemoryMB = data.totalMemory / (1024f * 1024f);
            sb.Clear();
            AppendFloat(sb, totalMemoryMB, 1);
            sb.Append("M");
            totalMemoryValueLabel.text = sb.ToString();
            
            // Graph current values
            sb.Clear();
            sb.Append(fps);
            currentFpsLabel.text = sb.ToString();
            
            sb.Clear();
            AppendFloat(sb, data.frameTime, 1);
            currentFrameTimeLabel.text = sb.ToString();
            
            sb.Clear();
            AppendFloat(sb, data.allocPerFrameMB, 2);
            sb.Append("M");
            currentAllocLabel.text = sb.ToString();

            // Update stats labels
            if (showExtendedStats)
            {
                // FPS Stats
                sb.Clear();
                sb.Append("min:");
                sb.Append(Mathf.RoundToInt(stats.fpsMin));
                sb.Append(" max:");
                sb.Append(Mathf.RoundToInt(stats.fpsMax));
                sb.Append(" avg:");
                sb.Append(Mathf.RoundToInt(stats.fpsAvg));
                fpsStatsLabel.text = sb.ToString();

                // Frame Time Stats
                sb.Clear();
                sb.Append("min:");
                AppendFloat(sb, stats.frameTimeMin, 1);
                sb.Append(" max:");
                AppendFloat(sb, stats.frameTimeMax, 1);
                sb.Append(" avg:");
                AppendFloat(sb, stats.frameTimeAvg, 1);
                frameTimeStatsLabel.text = sb.ToString();

                // Alloc Stats
                sb.Clear();
                sb.Append("min:");
                AppendFloat(sb, stats.allocMin, 1);
                sb.Append("K max:");
                AppendFloat(sb, stats.allocMax, 1);
                sb.Append("K avg:");
                AppendFloat(sb, stats.allocAvg, 1);
                sb.Append("K");
                allocStatsLabel.text = sb.ToString();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AppendFloat(StringBuilder sb, float value, int decimals)
        {
            if (value < 0f)
            {
                sb.Append('-');
                value = -value;
            }
        
            int intPart = (int)value;
            sb.Append(intPart);
        
            if (decimals > 0)
            {
                sb.Append('.');
                float fracPart = value - intPart;
            
                for (int i = 0; i < decimals; i++)
                {
                    fracPart *= 10f;
                    int digit = (int)fracPart;
                    sb.Append(digit);
                    fracPart -= digit;
                }
            }
        }

        private void TogglePanel()
        {
            isExpanded = !isExpanded;
            contentContainer.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            toggleButton.text = isExpanded ? "\u25b2" : "\u25bc";
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        
            if (mainContainer != null && mainContainer.parent != null)
            {
                mainContainer.RemoveFromHierarchy();
            }
        }
    }
}