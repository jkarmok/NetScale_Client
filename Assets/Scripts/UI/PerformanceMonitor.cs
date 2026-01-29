using UnityEngine;
using UnityEngine.UIElements;
using Unity.Profiling;
using System.Text;

public class PerformanceMonitor : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private string _containerName = "performance-monitor-container";
    [SerializeField] private int _graphSampleCount = 100;
    [SerializeField] private float _updateInterval = 0.1f;
    [SerializeField] private bool _startMinimized = false;
    
    private VisualElement _root;
    private VisualElement _mainContainer;
    private VisualElement _contentContainer;
    private Button _toggleButton;
    private Label _fpsLabel;
    private Label _frameTimeLabel;
    private Label _allocLabel;
    private Label _deviceInfoLabel;
    private VisualElement _frameTimeGraph;
    private VisualElement _allocGraph;

    private float[] _frameTimeSamples;
    private float[] _allocSamples;
    private int _currentIndex;

    private float _deltaTime;
    private float _fps;
    private float _frameTime;
    private long _lastAllocMemory;
    private long _currentAllocMemory;
    private float _allocPerFrame;

    private float _timer;

    private ProfilerRecorder _totalMemoryRecorder;
    private ProfilerRecorder _gcMemoryRecorder;
    
    private StringBuilder _sb;
    
    private const float GRAPH_HEIGHT = 120f;
    private const float MAX_FRAME_TIME = 33.33f;
    private const float MAX_ALLOC_MB = 1f;
    
    private bool _isExpanded = true;

    private void Awake()
    {
        _frameTimeSamples = new float[_graphSampleCount];
        _allocSamples = new float[_graphSampleCount];
        _sb = new StringBuilder(256);
        
        _totalMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
        _gcMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
        
        if (_uiDocument == null)
        {
            _uiDocument = GetComponent<UIDocument>();
        }
        
        if (_uiDocument != null)
        {
            InitializeUI();
            
            if (_startMinimized)
            {
                TogglePanel();
            }
        }
        else
        {
            Debug.LogError("PerformanceMonitor: UIDocument not found!");
            enabled = false;
        }
    }

    private void InitializeUI()
    {
        _root = _uiDocument.rootVisualElement;

        var targetContainer = string.IsNullOrEmpty(_containerName) ? _root : _root.Q<VisualElement>(_containerName);
        
        if (targetContainer == null)
        {
            Debug.LogWarning($"Container '{_containerName}' not found. Adding to root element.");
            targetContainer = _root;
        }
        
        _mainContainer = new VisualElement();
        _mainContainer.name = "performance-monitor";
        _mainContainer.style.backgroundColor = new Color(0, 0, 0, 0.85f);
        _mainContainer.style.borderTopLeftRadius = 8;
        _mainContainer.style.borderTopRightRadius = 8;
        _mainContainer.style.borderBottomLeftRadius = 8;
        _mainContainer.style.borderBottomRightRadius = 8;
        _mainContainer.style.borderTopWidth = 2;
        _mainContainer.style.borderBottomWidth = 2;
        _mainContainer.style.borderLeftWidth = 2;
        _mainContainer.style.borderRightWidth = 2;
        _mainContainer.style.borderTopColor = new Color(0, 0.5f, 1f, 0.8f);
        _mainContainer.style.borderBottomColor = new Color(0, 0.5f, 1f, 0.8f);
        _mainContainer.style.borderLeftColor = new Color(0, 0.5f, 1f, 0.8f);
        _mainContainer.style.borderRightColor = new Color(0, 0.5f, 1f, 0.8f);
        _mainContainer.style.marginBottom = 10;
        _mainContainer.style.marginLeft = 10;
        _mainContainer.style.marginTop = 10;
        _mainContainer.style.maxWidth = 400;
        _mainContainer.style.minWidth = 300;
        
        _mainContainer.pickingMode = PickingMode.Position;
        
        targetContainer.Add(_mainContainer);

        _toggleButton = new Button();
        _toggleButton.text = "▼ Performance Monitor";
        _toggleButton.style.backgroundColor = new Color(0, 0.3f, 0.6f, 1f);
        _toggleButton.style.color = Color.white;
        _toggleButton.style.fontSize = 14;
        _toggleButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        _toggleButton.style.borderTopWidth = 0;
        _toggleButton.style.borderBottomWidth = 1;
        _toggleButton.style.borderLeftWidth = 0;
        _toggleButton.style.borderRightWidth = 0;
        _toggleButton.style.borderBottomColor = new Color(0, 0.5f, 1f, 0.8f);
        _toggleButton.style.paddingTop = 8;
        _toggleButton.style.paddingBottom = 8;
        _toggleButton.style.paddingLeft = 10;
        _toggleButton.style.paddingRight = 10;
        _toggleButton.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
        _toggleButton.clicked += TogglePanel;
        _mainContainer.Add(_toggleButton);

        _contentContainer = new VisualElement();
        _contentContainer.style.paddingLeft = 10;
        _contentContainer.style.paddingRight = 10;
        _contentContainer.style.paddingTop = 5;
        _contentContainer.style.paddingBottom = 5;
        _mainContainer.Add(_contentContainer);

        var metricsContainer = new VisualElement();
        metricsContainer.style.flexDirection = FlexDirection.Row;
        metricsContainer.style.marginBottom = 10;
        
        _fpsLabel = CreateLabel("FPS: 0", 18, Color.green);
        _fpsLabel.style.marginBottom = 3;
        metricsContainer.Add(_fpsLabel);

        _frameTimeLabel = CreateLabel("Frame: 0.00 ms", 14, Color.cyan);
        _frameTimeLabel.style.marginBottom = 3;
        metricsContainer.Add(_frameTimeLabel);
        
        _allocLabel = CreateLabel("Alloc: 0.00 KB/frame", 14, Color.yellow);
        _allocLabel.style.marginBottom = 5;
        metricsContainer.Add(_allocLabel);
        
        _contentContainer.Add(metricsContainer);

        var frameTimeContainer = CreateGraphContainer("Frame Time (ms)");
        _frameTimeGraph = CreateGraph();
        frameTimeContainer.Add(_frameTimeGraph);
        _contentContainer.Add(frameTimeContainer);

        var allocContainer = CreateGraphContainer("Memory Allocations (MB)");
        _allocGraph = CreateGraph();
        allocContainer.Add(_allocGraph);
        _contentContainer.Add(allocContainer);

        _deviceInfoLabel = CreateLabel(GetDeviceInfo(), 10, new Color(0.8f, 0.8f, 0.8f, 1f));
        _deviceInfoLabel.style.marginTop = 8;
        _deviceInfoLabel.style.whiteSpace = WhiteSpace.Normal;
        _deviceInfoLabel.style.maxHeight = 60;
        _deviceInfoLabel.style.overflow = Overflow.Hidden;
        _contentContainer.Add(_deviceInfoLabel);
    }

    private void TogglePanel()
    {
        _isExpanded = !_isExpanded;
        _contentContainer.style.display = _isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
        _toggleButton.text = _isExpanded ? "▼ Performance Monitor" : "▶ Performance Monitor";
    }

    private Label CreateLabel(string text, int fontSize, Color color)
    {
        var label = new Label(text);
        label.style.fontSize = fontSize;
        label.style.color = color;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.whiteSpace = WhiteSpace.Normal;
        return label;
    }

    private VisualElement CreateGraphContainer(string title)
    {
        var container = new VisualElement();
        container.style.marginBottom = 8;
        
        var titleLabel = new Label(title);
        titleLabel.style.fontSize = 12;
        titleLabel.style.color = Color.white;
        titleLabel.style.marginBottom = 3;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        container.Add(titleLabel);
        
        return container;
    }

    private VisualElement CreateGraph()
    {
        var graph = new VisualElement();
        graph.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
        graph.style.height = GRAPH_HEIGHT;
        graph.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        graph.style.borderTopWidth = 1;
        graph.style.borderBottomWidth = 1;
        graph.style.borderLeftWidth = 1;
        graph.style.borderRightWidth = 1;
        graph.style.borderTopColor = Color.gray;
        graph.style.borderBottomColor = Color.gray;
        graph.style.borderLeftColor = Color.gray;
        graph.style.borderRightColor = Color.gray;
        graph.style.marginBottom = 5;
        
        graph.generateVisualContent += OnGenerateVisualContent;
        
        return graph;
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        var graph = ctx.visualElement;
        
        if (graph == _frameTimeGraph)
        {
            DrawGraph(ctx, _frameTimeSamples, MAX_FRAME_TIME, new Color(0, 1, 1, 0.9f));
        }
        else if (graph == _allocGraph)
        {
            DrawGraph(ctx, _allocSamples, MAX_ALLOC_MB, new Color(1, 1, 0, 0.9f));
        }
    }

    private void DrawGraph(MeshGenerationContext ctx, float[] samples, float maxValue, Color color)
    {
        var painter = ctx.painter2D;
        var rect = ctx.visualElement.contentRect;
        
        if (rect.width < 2 || rect.height < 2) return;
        
        painter.strokeColor = color;
        painter.lineWidth = 2f;
        painter.BeginPath();
        
        float stepX = rect.width / (_graphSampleCount - 1);
        bool firstPoint = true;
        
        for (int i = 0; i < _graphSampleCount; i++)
        {
            int sampleIndex = (_currentIndex + i) % _graphSampleCount;
            float value = samples[sampleIndex];
            float normalizedValue = Mathf.Clamp01(value / maxValue);
            
            float x = i * stepX;
            float y = rect.height - (normalizedValue * rect.height);
            
            if (firstPoint)
            {
                painter.MoveTo(new Vector2(x, y));
                firstPoint = false;
            }
            else
            {
                painter.LineTo(new Vector2(x, y));
            }
        }
        
        painter.Stroke();
    }

    private void Update()
    {
        _deltaTime = Time.unscaledDeltaTime;
        
        _fps = 1f / _deltaTime;
        _frameTime = _deltaTime * 1000f;
        
        _currentAllocMemory = _gcMemoryRecorder.LastValue;
        long allocDelta = _currentAllocMemory - _lastAllocMemory;
        _allocPerFrame = allocDelta / 1024f;
        _lastAllocMemory = _currentAllocMemory;
 
        _frameTimeSamples[_currentIndex] = _frameTime;
        _allocSamples[_currentIndex] = Mathf.Max(0, _allocPerFrame) / 1024f; // В MB
 
        _timer += _deltaTime;
        if (_timer >= _updateInterval)
        {
            _timer = 0f;
            UpdateUI();
        }
 
        if (_isExpanded)
        {
            _frameTimeGraph.MarkDirtyRepaint();
            _allocGraph.MarkDirtyRepaint();
        }
    }

    private void UpdateUI()
    {
        if (!_isExpanded) return;
 
        _sb.Clear();
        _sb.Append("FPS: ");
        _sb.Append(Mathf.RoundToInt(_fps));
        _fpsLabel.text = _sb.ToString();
        
        if (_fps >= 55f)
            _fpsLabel.style.color = Color.green;
        else if (_fps >= 30f)
            _fpsLabel.style.color = Color.yellow;
        else
            _fpsLabel.style.color = Color.red;
        
        // Frame Time
        _sb.Clear();
        _sb.Append("Frame: ");
        _sb.Append(_frameTime.ToString("F2"));
        _sb.Append(" ms");
        _frameTimeLabel.text = _sb.ToString();
        
        // Allocations
        _sb.Clear();
        _sb.Append("Alloc: ");
        if (_allocPerFrame >= 1024f)
        {
            _sb.Append((_allocPerFrame / 1024f).ToString("F2"));
            _sb.Append(" MB/frame");
        }
        else
        {
            _sb.Append(_allocPerFrame.ToString("F2"));
            _sb.Append(" KB/frame");
        }
        _allocLabel.text = _sb.ToString();
    }

    private string GetDeviceInfo()
    {
        _sb.Clear();
        _sb.AppendLine("=== DEVICE INFO ===");
        _sb.Append("Device: ");
        _sb.AppendLine(SystemInfo.deviceModel);
        _sb.Append("OS: ");
        _sb.AppendLine(SystemInfo.operatingSystem);
        _sb.Append("CPU: ");
        _sb.Append(SystemInfo.processorType);
        _sb.Append(" (");
        _sb.Append(SystemInfo.processorCount);
        _sb.AppendLine(" cores)");
        _sb.Append("GPU: ");
        _sb.AppendLine(SystemInfo.graphicsDeviceName);
        _sb.Append("VRAM: ");
        _sb.Append(SystemInfo.graphicsMemorySize);
        _sb.AppendLine(" MB");
        _sb.Append("RAM: ");
        _sb.Append(SystemInfo.systemMemorySize);
        _sb.AppendLine(" MB");
        _sb.Append("Screen: ");
        _sb.Append(Screen.width);
        _sb.Append("x");
        _sb.Append(Screen.height);
        _sb.Append(" @ ");
        _sb.Append(Screen.currentResolution.refreshRate);
        _sb.Append("Hz");
        
        return _sb.ToString();
    }

    private void OnDestroy()
    {
        _totalMemoryRecorder.Dispose();
        _gcMemoryRecorder.Dispose();
 
        if (_mainContainer != null && _mainContainer.parent != null)
        {
            _mainContainer.RemoveFromHierarchy();
        }
    }
 
    public VisualElement GetContainer()
    {
        return _mainContainer;
    }
}