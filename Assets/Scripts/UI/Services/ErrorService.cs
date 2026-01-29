using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Services
{
    public class ErrorService : IErrorService
    {
        private readonly ErrorServiceConfig _config;

        private readonly VisualTreeAsset _inlineErrorTemplate;
        private readonly VisualTreeAsset _popupErrorTemplate;
        
        private const string DEFAULT_ERROR_CONTAINER = "ErrorContainer";
        private const string DEFAULT_POPUP_CONTAINER = "PopupErrorContainer";

        public ErrorService(
            VisualTreeAsset inlineErrorTemplate, 
            VisualTreeAsset popupErrorTemplate,
            ErrorServiceConfig config) 
        {
            _inlineErrorTemplate = inlineErrorTemplate;
            _popupErrorTemplate = popupErrorTemplate;
            _config = config;
        }
        
        public void ShowError(VisualElement root, string message, ErrorType type = ErrorType.Popup, 
                             ErrorPosition position = ErrorPosition.TopRight, float duration = -1)
        {
            if (root == null)
            {
                Debug.LogWarning("Cannot show error: root element is null");
                return;
            }

            switch (type)
            {
                case ErrorType.Inline:
                    ShowInlineError(root, message, duration);
                    break;
                case ErrorType.Popup:
                    ShowPopupError(root, message, position, duration);
                    break;
            }
        }
        
        public void ShowInlineError(VisualElement root, string message, float duration = -1)
        {
            var container = GetOrCreateContainer(root, DEFAULT_ERROR_CONTAINER);
            container.Clear();

            VisualElement errorElement = CreateInlineErrorElement(message);
            if (errorElement == null) return;

            container.Add(errorElement);

            // Автоочистка
            float actualDuration = GetActualDuration(duration, _config.DefaultInlineDuration);
            if (actualDuration > 0)
            {
                RemoveElementAfterDelay(container, errorElement, actualDuration);
            }
        }
        
        public void ShowPopupError(VisualElement root, string message, ErrorPosition position = ErrorPosition.TopRight, 
                                  float duration = -1)
        {
            var container = GetOrCreatePopupContainer(root);
            container.style.display = DisplayStyle.Flex;

            VisualElement popupElement = null;
            popupElement = CreatePopupErrorElement(message, () => RemoveError(container, popupElement));
            if (popupElement == null) return;
            
            // Позиционирование
            ApplyPosition(popupElement, position);
            container.Add(popupElement);

            // Автоочистка
            float actualDuration = GetActualDuration(duration, _config.DefaultPopupDuration);
            if (actualDuration > 0)
            {
                RemoveElementAfterDelay(container, popupElement, actualDuration);
            }
        }
        
        public void ClearErrors(VisualElement root, ErrorType? type = null)
        {
            if (type == null || type == ErrorType.Inline)
            {
                var container = root.Q<VisualElement>(DEFAULT_ERROR_CONTAINER);
                container?.Clear();
            }

            if (type == null || type == ErrorType.Popup)
            {
                var container = root.Q<VisualElement>(DEFAULT_POPUP_CONTAINER);
                if (container != null)
                {
                    container.Clear();
                    container.style.display = DisplayStyle.None;
                }
            }
        }
        
        // Приватные вспомогательные методы
        private VisualElement CreateInlineErrorElement(string message)
        {
            if (_inlineErrorTemplate == null)
            {
                Debug.LogWarning("Inline error template not provided");
                return CreateFallbackInline(message);
            }
            
            var element = _inlineErrorTemplate.CloneTree();
            element.style.display = DisplayStyle.Flex;

            var errorText = element.Q<Label>("ErrorText");
            if (errorText != null)
                errorText.text = message;

            return element;
        }
        
        private VisualElement CreatePopupErrorElement(
            string message,
            System.Action onClose)
        {
            if (_popupErrorTemplate == null)
                return CreateFallbackPopup(message, onClose);

            var element = _popupErrorTemplate.CloneTree();
            element.style.display = DisplayStyle.Flex;

            var errorText = element.Q<Label>("PopupErrorText");
            var closeButton = element.Q<Button>("CloseButton");

            if (errorText != null)
                errorText.text = message;

            if (closeButton != null)
                closeButton.clicked += onClose;

            return element;
        }
        
        private VisualElement GetOrCreateContainer(VisualElement root, string name)
        {
            var container = root.Q<VisualElement>(name);
            if (container == null)
            {
                container = new VisualElement { name = name };
                root.Add(container);
            }
            return container;
        }
        
        private VisualElement GetOrCreatePopupContainer(VisualElement root)
        {
            var container = root.Q<VisualElement>(DEFAULT_POPUP_CONTAINER);
            if (container == null)
            {
                container = new VisualElement
                {
                    name = DEFAULT_POPUP_CONTAINER,
                    style =
                    {
                        position = Position.Absolute,
                        top = 0,
                        left = 0,
                        width = Length.Percent(100),
                        height = Length.Percent(100),
                        display = DisplayStyle.None
                    }
                };
                root.Add(container);
            }
            return container;
        }
        
        private void ApplyPosition(VisualElement element, ErrorPosition position)
        {
            element.style.position = Position.Absolute;
 
            switch (position)
            {
                case ErrorPosition.TopCenter:
                    element.style.top = 20;
                    element.style.left = Length.Percent(50);
                    element.style.translate = new StyleTranslate(new Translate(Length.Percent(-50), 0));
                    break;
                case ErrorPosition.TopRight:
                    element.style.top = 20;
                    element.style.right = 20;
                    break;
                case ErrorPosition.TopLeft:
                    element.style.top = 20;
                    element.style.left = 20;
                    break;
                case ErrorPosition.BottomCenter:
                    element.style.bottom = 20;
                    element.style.left = Length.Percent(50);
                    element.style.translate = new StyleTranslate(new Translate(Length.Percent(-50), 0));
                    break;
                case ErrorPosition.Inline:
                    element.style.position = Position.Relative;
                    break;
            }
        }
        
        private async void RemoveElementAfterDelay(VisualElement container, VisualElement element, float delay)
        {
            await Task.Delay((int)(delay * 1000));
            RemoveError(container, element);
        }
        
        private void RemoveError(VisualElement container, VisualElement errorElement)
        {
            if (errorElement != null && errorElement.parent != null)
            {
                errorElement.parent.Remove(errorElement);
                
                if (container.name == DEFAULT_POPUP_CONTAINER && container.childCount == 0)
                {
                    container.style.display = DisplayStyle.None;
                }
            }
        }
        
        private float GetActualDuration(float requestedDuration, float defaultDuration)
        {
            return requestedDuration >= 0 ? requestedDuration : defaultDuration;
        }
        
        // Fallback элементы
        private VisualElement CreateFallbackInline(string message)
        {
            var element = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    backgroundColor = new Color(0.86f, 0.21f, 0.27f, 0.1f),
                    paddingTop = 8,
                    paddingBottom = 8,
                    paddingLeft = 12,
                    paddingRight = 12,
                    marginTop = 4,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = new Color(0.86f, 0.21f, 0.27f, 0.3f),
                    borderBottomColor = new Color(0.86f, 0.21f, 0.27f, 0.3f),
                    borderLeftColor = new Color(0.86f, 0.21f, 0.27f, 0.3f),
                    borderRightColor = new Color(0.86f, 0.21f, 0.27f, 0.3f),
                }
            };

            var icon = new Label("!")
            {
                style =
                {
                    width = 18,
                    height = 18,
                    backgroundColor = new Color(0.86f, 0.21f, 0.27f, 1f),
                    color = Color.white,
                    fontSize = 12,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    marginRight = 8
                }
            };

            var label = new Label(message)
            {
                style =
                {
                    color = new Color(0.86f, 0.21f, 0.27f, 1f),
                    fontSize = 12,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            element.Add(icon);
            element.Add(label);
            return element;
        }
        
        private VisualElement CreateFallbackPopup(string message, System.Action onClose)
        {
            var element = new VisualElement
            {
                style =
                {
                    width = 300,
                    backgroundColor = new Color(0.86f, 0.21f, 0.27f, 0.95f),
                    paddingTop = 16,
                    paddingBottom = 16,
                    paddingLeft = 20,
                    paddingRight = 20,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = new Color(0.96f, 0.31f, 0.37f, 1f),
                    borderBottomColor = new Color(0.96f, 0.31f, 0.37f, 1f),
                    borderLeftColor = new Color(0.96f, 0.31f, 0.37f, 1f),
                    borderRightColor = new Color(0.96f, 0.31f, 0.37f, 1f)
                }
            };

            var closeButton = new Button(onClose)
            {
                text = "✕",
                style =
                {
                    position = Position.Absolute,
                    top = 8,
                    right = 8,
                    width = 24,
                    height = 24,
                    paddingTop = 0,
                    paddingBottom = 0,
                    backgroundColor = new Color(1, 1, 1, 0.1f),
                    color = Color.white
                }
            };

            var label = new Label(message)
            {
                style =
                {
                    color = Color.white,
                    whiteSpace = WhiteSpace.Normal,
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            element.Add(closeButton);
            element.Add(label);
            return element;
        }
    }

    public class ErrorServiceConfig
    {
        public float DefaultPopupDuration { get; set; } = 3f;
        public float DefaultInlineDuration { get; set; } = 5f;
    }
}