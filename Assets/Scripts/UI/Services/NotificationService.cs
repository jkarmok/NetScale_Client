using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Services
{
    [System.Serializable]
    public class NotificationServiceConfig
    {
        public float DefaultPopupDuration = 3f;
        public float DefaultInlineDuration = 5f;
        public float DefaultToastDuration = 2.5f;

        public Color ErrorColor = new Color(0.86f, 0.21f, 0.27f, 1f); // #dc3545
        public Color WarningColor = new Color(1f, 0.65f, 0f, 1f); // #ffa500
        public Color InfoColor = new Color(0.13f, 0.47f, 0.95f, 1f); // #217aff
        public Color SuccessColor = new Color(0.25f, 0.69f, 0.45f, 1f); // #40b16d
    }
    
    public class NotificationService : INotificationService
    {
        private readonly NotificationServiceConfig _config;
        private readonly UIDocument _uiDocument;
        private VisualElement Root => _uiDocument.rootVisualElement;
        private readonly VisualTreeAsset _inlineTemplate;
        private readonly VisualTreeAsset _popupTemplate;

        private const string INLINE_CONTAINER = "NotificationContainer";
        private const string POPUP_CONTAINER = "PopupNotificationContainer";
        
        private readonly Dictionary<VisualElement, VisualElement> _containerCache = new();
        private readonly HashSet<VisualElement> _pendingRemoval = new();
        
        private readonly Dictionary<NotificationPosition, List<VisualElement>> _popupNotifications = new();
        
        private static readonly Dictionary<NotificationType, string> _icons = new()
        {
            { NotificationType.Error, "!" },
            { NotificationType.Warning, "⚠" },
            { NotificationType.Info, "i" },
            { NotificationType.Success, "✓" }
        };

        private static readonly Dictionary<NotificationType, string> _titles = new()
        {
            { NotificationType.Error, "Error" },
            { NotificationType.Warning, "Warning" },
            { NotificationType.Info, "Information" },
            { NotificationType.Success, "Success" }
        };

        public NotificationService(
            VisualTreeAsset inlineTemplate,
            VisualTreeAsset popupTemplate,
            NotificationServiceConfig config,
            UIDocument uiDocument)
        {
            _inlineTemplate = inlineTemplate;
            _popupTemplate = popupTemplate;
            _config = config;
            _uiDocument = uiDocument;


            foreach (NotificationPosition position in System.Enum.GetValues(typeof(NotificationPosition)))
            {
                _popupNotifications[position] = new List<VisualElement>();
            }
        }

        public void ShowNotification(
            string message,
            NotificationType type = NotificationType.Info,
            NotificationDisplayType displayType = NotificationDisplayType.Popup,
            NotificationPosition position = NotificationPosition.TopRight,
            float duration = -1,
            string title = null)
        {
            if (Root == null)
            {
                Debug.LogWarning("Cannot show notification: root element is null");
                return;
            }

            switch (displayType)
            {
                case NotificationDisplayType.Inline:
                    ShowInlineNotification(message, type, duration);
                    break;
                case NotificationDisplayType.Popup:
                    ShowPopupNotification(message, type, position, duration, title);
                    break;
                case NotificationDisplayType.Toast:
                    ShowToastNotification(message, type, position, duration);
                    break;
            }
        }

        public void ShowInlineNotification(
            string message,
            NotificationType type = NotificationType.Info,
            float duration = -1)
        {
            var container = GetOrCreateContainer(Root, INLINE_CONTAINER);

            var notificationElement = CreateInlineNotificationElement(message, type);
            if (notificationElement == null) return;

            container.Add(notificationElement);

            float actualDuration = GetActualDuration(duration, _config.DefaultInlineDuration);
            if (actualDuration > 0)
            {
                RemoveElementAfterDelay(container, notificationElement, actualDuration);
            }
        }

        public void ShowPopupNotification(
            string message,
            NotificationType type = NotificationType.Info,
            NotificationPosition position = NotificationPosition.TopRight,
            float duration = -1,
            string title = null)
        {
            var container = GetOrCreatePopupContainer(Root);
            container.style.display = DisplayStyle.Flex;

            VisualElement popupElement = null;
            popupElement = CreatePopupNotificationElement(
                message,
                type,
                title ?? GetTitle(type),
                () => RemoveNotification(container, popupElement, position));

            if (popupElement == null) return;
            
            _popupNotifications[position].Add(popupElement);
            
            UpdatePopupPositions(position);
            container.Add(popupElement);
            
            float actualDuration = GetActualDuration(duration, _config.DefaultPopupDuration);
            if (actualDuration > 0)
            {
                RemoveElementAfterDelay(container, popupElement, actualDuration, position);
            }
        }

        public void ShowToastNotification(
            string message,
            NotificationType type = NotificationType.Info,
            NotificationPosition position = NotificationPosition.BottomRight,
            float duration = -1)
        {
            var container = GetOrCreatePopupContainer(Root);
            container.style.display = DisplayStyle.Flex;

            var toastElement = CreateToastNotificationElement(message, type);
            if (toastElement == null) return;
            
            _popupNotifications[position].Add(toastElement);
            
            UpdatePopupPositions(position);
            container.Add(toastElement);

            float actualDuration = GetActualDuration(duration, _config.DefaultToastDuration);
            if (actualDuration > 0)
            {
                RemoveElementAfterDelay(container, toastElement, actualDuration, position);
            }
        }

        public void ClearNotifications(
            NotificationType? type = null,
            NotificationDisplayType? displayType = null)
        {
            if (!displayType.HasValue || displayType == NotificationDisplayType.Inline)
            {
                var container = Root.Q<VisualElement>(INLINE_CONTAINER);
                if (container != null)
                {
                    if (type.HasValue)
                    {
                        var typeString = $"notification-{type.Value.ToString().ToLower()}";
                        var toRemove = new List<VisualElement>();
                        
                        foreach (var child in container.Children())
                        {
                            if (child.ClassListContains(typeString))
                            {
                                toRemove.Add(child);
                            }
                        }

                        foreach (var child in toRemove)
                        {
                            container.Remove(child);
                        }
                    }
                    else
                    {
                        container.Clear();
                    }
                }
            }

            if (!displayType.HasValue ||
                displayType == NotificationDisplayType.Popup ||
                displayType == NotificationDisplayType.Toast)
            {
                var container = Root.Q<VisualElement>(POPUP_CONTAINER);
                if (container != null)
                {
                    if (type.HasValue)
                    {
                        var typeString = $"popup-notification-{type.Value.ToString().ToLower()}";
                        var toRemove = new List<VisualElement>();
                        
                        foreach (var child in container.Children())
                        {
                            if (child.ClassListContains(typeString))
                            {
                                toRemove.Add(child);
                                RemoveFromPositionLists(child);
                            }
                        }

                        foreach (var child in toRemove)
                        {
                            container.Remove(child);
                        }
                        
                        UpdateAllPopupPositions();

                        if (container.childCount == 0)
                        {
                            container.style.display = DisplayStyle.None;
                        }
                    }
                    else
                    {
                        foreach (var positionList in _popupNotifications.Values)
                        {
                            positionList.Clear();
                        }
                        
                        container.Clear();
                        container.style.display = DisplayStyle.None;
                    }
                }
            }
        }

        private void RemoveFromPositionLists(VisualElement element)
        {
            foreach (var positionList in _popupNotifications.Values)
            {
                positionList.Remove(element);
            }
        }

        private void UpdateAllPopupPositions()
        {
            foreach (var position in _popupNotifications.Keys)
            {
                UpdatePopupPositions(position);
            }
        }

        private void UpdatePopupPositions(NotificationPosition position)
        {
            var notifications = _popupNotifications[position];
            
            float currentOffset = 20f;
            
            foreach (var notification in notifications)
            {
                if (notification == null || notification.parent == null)
                    continue;
                    
                ApplyPositionWithOffset(notification, position, currentOffset);
                
                currentOffset += GetElementHeight(notification) + 10;
            }
        }

        private VisualElement CreateInlineNotificationElement(string message, NotificationType type)
        {
            if (_inlineTemplate == null)
                return CreateFallbackInline(message, type);

            var element = _inlineTemplate.CloneTree();
            var root = element.Q<VisualElement>("NotificationRoot");

            root.AddToClassList($"notification-{type.ToString().ToLower()}");
            
            var iconText = element.Q<Label>("IconText");
            if (iconText != null && _icons.TryGetValue(type, out var icon))
                iconText.text = icon;
            
            var notificationText = element.Q<Label>("NotificationText");
            if (notificationText != null)
                notificationText.text = message;

            return root;
        }

        private VisualElement CreatePopupNotificationElement(
            string message,
            NotificationType type,
            string title,
            System.Action onClose)
        {
            if (_popupTemplate == null)
                return CreateFallbackPopup(message, type, title, onClose);

            var element = _popupTemplate.CloneTree();
            var root = element.Q<VisualElement>("NotificationRoot");
            
            root.AddToClassList($"popup-notification-{type.ToString().ToLower()}");
            
            var typeIcon = element.Q<Label>("TypeIcon");
            if (typeIcon != null && _icons.TryGetValue(type, out var icon))
                typeIcon.text = icon;
            
            var titleLabel = element.Q<Label>("NotificationTitle");
            if (titleLabel != null)
                titleLabel.text = title;
            
            var notificationText = element.Q<Label>("NotificationText");
            if (notificationText != null)
                notificationText.text = message;

            var closeButton = element.Q<Button>("CloseButton");
            if (closeButton != null)
                closeButton.clicked += onClose;

            return root;
        }

        private VisualElement CreateToastNotificationElement(string message, NotificationType type)
        {
            var element = new VisualElement();
            element.AddToClassList("toast-notification");
            element.AddToClassList($"popup-notification-{type.ToString().ToLower()}");
            
            var icon = new Label(_icons.TryGetValue(type, out var iconStr) ? iconStr : "i");
            icon.style.width = 16;
            icon.style.height = 16;
            icon.style.unityTextAlign = TextAnchor.MiddleCenter;
            icon.style.fontSize = 10;
            icon.style.color = Color.white;
            icon.style.backgroundColor = GetNotificationColor(type);
            icon.AddToClassList("toast-icon");
            
            var text = new Label(message);
            text.style.fontSize = 13;
            text.style.color = type == NotificationType.Warning ? Color.black : Color.white;
            text.style.whiteSpace = WhiteSpace.Normal;
            text.AddToClassList("toast-text");

            element.Add(icon);
            element.Add(text);

            return element;
        }

        private VisualElement GetOrCreateContainer(VisualElement root, string name)
        {
            if (!_containerCache.TryGetValue(root, out var container))
            {
                container = root.Q<VisualElement>(name);
                if (container == null)
                {
                    container = new VisualElement { name = name };
                    container.style.flexDirection = FlexDirection.Column;
                    container.AddToClassList("notification-container");
                    root.Add(container);
                }
                _containerCache[root] = container;
            }

            return container;
        }

        private VisualElement GetOrCreatePopupContainer(VisualElement root)
        {
            var container = GetOrCreateContainer(root, POPUP_CONTAINER);
            
            container.style.position = Position.Absolute;
            container.style.top = 0;
            container.style.left = 0;
            container.style.width = Length.Percent(100);
            container.style.height = Length.Percent(100);
            container.style.display = DisplayStyle.None;
            
            return container;
        }

        private void ApplyPositionWithOffset(VisualElement element, NotificationPosition position, float verticalOffset = 0)
        {
            element.style.position = Position.Absolute;
            
            element.style.top = StyleKeyword.Auto;
            element.style.bottom = StyleKeyword.Auto;
            element.style.left = StyleKeyword.Auto;
            element.style.right = StyleKeyword.Auto;
            element.style.translate = new StyleTranslate(new Translate(0, 0));

            switch (position)
            {
                case NotificationPosition.TopCenter:
                    element.style.top = 20 + verticalOffset;
                    element.style.left = Length.Percent(50);
                    element.style.translate = new StyleTranslate(new Translate(Length.Percent(-50), 0));
                    break;
                case NotificationPosition.TopRight:
                    element.style.top = 20 + verticalOffset;
                    element.style.right = 20;
                    break;
                case NotificationPosition.TopLeft:
                    element.style.top = 20 + verticalOffset;
                    element.style.left = 20;
                    break;
                case NotificationPosition.BottomCenter:
                    element.style.bottom = 20 + verticalOffset;
                    element.style.left = Length.Percent(50);
                    element.style.translate = new StyleTranslate(new Translate(Length.Percent(-50), 0));
                    break;
                case NotificationPosition.BottomRight:
                    element.style.bottom = 20 + verticalOffset;
                    element.style.right = 20;
                    break;
                case NotificationPosition.BottomLeft:
                    element.style.bottom = 20 + verticalOffset;
                    element.style.left = 20;
                    break;
                case NotificationPosition.Inline:
                    element.style.position = Position.Relative;
                    element.style.marginBottom = 8;
                    element.style.top = 0;
                    element.style.bottom = 0;
                    element.style.left = 0;
                    element.style.right = 0;
                    break;
            }
        }

        private float GetElementHeight(VisualElement element)
        {
            if (element.resolvedStyle.height > 0)
                return element.resolvedStyle.height;

            if (element.ClassListContains("toast-notification"))
                return 44f;
            
            return 120f;
        }

        private async void RemoveElementAfterDelay(VisualElement container, VisualElement element, float delay, NotificationPosition? position = null)
        {
            if (_pendingRemoval.Contains(element))
                return;
                
            _pendingRemoval.Add(element);
            
            await Task.Delay((int)(delay * 1000));
            
            if (position.HasValue)
            {
                RemoveNotification(container, element, position.Value);
            }
            else
            {
                RemoveNotification(container, element);
            }
            
            _pendingRemoval.Remove(element);
        }

        private void RemoveNotification(VisualElement container, VisualElement notificationElement, NotificationPosition? position = null)
        {
            if (notificationElement != null && notificationElement.parent != null)
            {
                notificationElement.parent.Remove(notificationElement);
                
                if (position.HasValue)
                {
                    _popupNotifications[position.Value].Remove(notificationElement);
                    UpdatePopupPositions(position.Value);
                }
                else
                {
                    RemoveFromPositionLists(notificationElement);
                    UpdateAllPopupPositions();
                }

                if (container.name == POPUP_CONTAINER && container.childCount == 0)
                {
                    container.style.display = DisplayStyle.None;
                }
            }
        }

        private float GetActualDuration(float requestedDuration, float defaultDuration)
        {
            return requestedDuration >= 0 ? requestedDuration : defaultDuration;
        }

        private string GetTitle(NotificationType type)
        {
            return _titles.TryGetValue(type, out var title) ? title : "Notification";
        }

        private Color GetNotificationColor(NotificationType type)
        {
            return type switch
            {
                NotificationType.Error => _config.ErrorColor,
                NotificationType.Warning => _config.WarningColor,
                NotificationType.Info => _config.InfoColor,
                NotificationType.Success => _config.SuccessColor,
                _ => _config.InfoColor
            };
        }

        private VisualElement CreateFallbackInline(string message, NotificationType type)
        {
            var color = GetNotificationColor(type);

            var element = new VisualElement();
            element.style.flexDirection = FlexDirection.Row;
            element.style.alignItems = Align.Center;
            element.style.backgroundColor = new Color(color.r, color.g, color.b, 0.1f);
            element.style.paddingTop = 8;
            element.style.paddingBottom = 8;
            element.style.paddingLeft = 12;
            element.style.paddingRight = 12;
            element.style.marginTop = 4;
            element.style.marginBottom = 8;
            element.style.borderLeftWidth = 3;
            element.style.borderLeftColor = color;

            var icon = new Label(_icons.TryGetValue(type, out var iconStr) ? iconStr : "i");
            icon.style.width = 18;
            icon.style.height = 18;
            icon.style.backgroundColor = color;
            icon.style.color = Color.white;
            icon.style.fontSize = 12;
            icon.style.unityTextAlign = TextAnchor.MiddleCenter;
            icon.style.marginRight = 8;

            var label = new Label(message);
            label.style.color = color;
            label.style.fontSize = 12;
            label.style.whiteSpace = WhiteSpace.Normal;

            element.Add(icon);
            element.Add(label);
            return element;
        }

        private VisualElement CreateFallbackPopup(
            string message,
            NotificationType type,
            string title,
            System.Action onClose)
        {
            var color = GetNotificationColor(type);
            var isWarning = type == NotificationType.Warning;
            var textColor = isWarning ? Color.black : Color.white;

            var element = new VisualElement();
            element.style.width = 300;
            element.style.backgroundColor = color;
            element.style.overflow = Overflow.Hidden;

            // Хедер
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.paddingTop = 12;
            header.style.paddingBottom = 12;
            header.style.paddingLeft = 16;
            header.style.paddingRight = 16;
            header.style.backgroundColor = new Color(0, 0, 0, isWarning ? 0.1f : 0.2f);

            var headerLeft = new VisualElement();
            headerLeft.style.flexDirection = FlexDirection.Row;
            headerLeft.style.alignItems = Align.Center;

            var typeIcon = new Label(_icons.TryGetValue(type, out var iconStr) ? iconStr : "i");
            typeIcon.style.width = 20;
            typeIcon.style.height = 20;
            typeIcon.style.backgroundColor = textColor;
            typeIcon.style.color = color;
            typeIcon.style.fontSize = 12;
            typeIcon.style.unityTextAlign = TextAnchor.MiddleCenter;

            var titleLabel = new Label(title);
            titleLabel.style.color = textColor;
            titleLabel.style.fontSize = 14;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            var closeButton = new Button(onClose);
            closeButton.text = "✕";
            closeButton.style.width = 24;
            closeButton.style.height = 24;
            closeButton.style.paddingTop = 0;
            closeButton.style.paddingBottom = 0;
            closeButton.style.backgroundColor = Color.clear;
            closeButton.style.color = textColor;

            // Текст
            var label = new Label(message);
            label.style.color = textColor;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = 13;
            label.style.paddingTop = 16;
            label.style.paddingBottom = 16;
            label.style.paddingLeft = 16;
            label.style.paddingRight = 16;

            headerLeft.Add(typeIcon);
            headerLeft.Add(titleLabel);
            header.Add(headerLeft);
            header.Add(closeButton);

            element.Add(header);
            element.Add(label);

            return element;
        }
    }
}