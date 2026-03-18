// LoginScreen.cs
using System;
using UI.Services;
using UnityEngine.UIElements;

namespace UI.Screens
{
    public class LoginScreen : UIScreen
    {
        private readonly INotificationService _notificationService;
        private Button _connectButton;
        private TextField _connectionInput;
        private VisualElement _errorContainer;
        
        private const string CONNECT_BUTTON_ID = "ConnectionButton";
        private const string CONNECTION_INPUT_ID = "ConnectionInput";
        private const string ERROR_CONTAINER_ID = "ErrorContainer";
        
        private const char CONNECTION_SEPARATOR = ':';
        private static readonly string[] ExpectedConnectionParts = { "host", "port" };
        
        public event Action<string, int> OnConnectSubmitted; 
        
        public LoginScreen(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }
        
        public override void Initialize(VisualElement topElement, VisualElement rootElement)
        {
            base.Initialize(topElement, rootElement);
            
            _connectButton = m_TopElement.Q<Button>(CONNECT_BUTTON_ID);
            _connectionInput = m_TopElement.Q<TextField>(CONNECTION_INPUT_ID);
            _errorContainer = GetOrCreateErrorContainer();
            
            _connectButton.clicked += OnConnectButtonClicked;
            _connectionInput.RegisterCallback<FocusInEvent>(OnInputFocusIn);
        }
        
        private VisualElement GetOrCreateErrorContainer()
        {
            var container = m_TopElement.Q<VisualElement>(ERROR_CONTAINER_ID);
            if (container == null)
            {
                container = new VisualElement { name = ERROR_CONTAINER_ID };
                var inputContainer = _connectionInput.parent;
                var inputIndex = inputContainer.IndexOf(_connectionInput);
                inputContainer.Insert(inputIndex + 1, container);
            }
            return container;
        }
        
        private void OnInputFocusIn(FocusInEvent evt)
        {
            ClearErrors();
            _connectionInput.RemoveFromClassList("field-error");
        }

        private void ClearErrors()
        {
            _notificationService.ClearNotifications(NotificationType.Error, NotificationDisplayType.Inline);
        }

        private void ShowInlineError(string errorMessage)
        {
            _notificationService.ShowInlineNotification(errorMessage, NotificationType.Error);
        }

        private void OnConnectButtonClicked()
        {
            ClearErrors();
            
            if (string.IsNullOrWhiteSpace(_connectionInput.value))
            {
                ShowValidationError("Please enter server address");
                return;
            }
    
            var parts = _connectionInput.value.Split(CONNECTION_SEPARATOR);
    
            if (parts.Length != 2)
            {
                ShowValidationError($"Format: host{CONNECTION_SEPARATOR}port");
                return;
            }
    
            if (!int.TryParse(parts[1], out int port) || port <= 0 || port > 65535)
            {
                ShowValidationError("Port must be between 1 and 65535");
                return;
            }
 
            OnConnectSubmitted?.Invoke(parts[0], port);
        }

        private void ShowValidationError(string errorMessage)
        {
            ShowInlineError(errorMessage);
            _connectionInput.AddToClassList("field-error");
        }
 
        public override void Dispose()
        {
            base.Dispose();
            
            if (_connectButton != null)
                _connectButton.clicked -= OnConnectButtonClicked;
            
            if (_connectionInput != null)
                _connectionInput.UnregisterCallback<FocusInEvent>(OnInputFocusIn);
            
            OnConnectSubmitted = null;
        }
    }
}