using UnityEngine.UIElements;

namespace UI.Services
{
    public enum ErrorType
    {
        Inline,
        Popup
    }

    public enum ErrorPosition
    {
        TopCenter,
        TopRight,
        TopLeft,
        BottomCenter,
        Inline
    }

    public interface IErrorService
    {
        void ShowError(VisualElement root, string message, ErrorType type = ErrorType.Popup, 
            ErrorPosition position = ErrorPosition.TopRight, float duration = -1);
        
        void ShowInlineError(VisualElement root, string message, float duration = -1);
        void ShowPopupError(VisualElement root, string message, ErrorPosition position = ErrorPosition.TopRight, 
            float duration = -1);
        void ClearErrors(VisualElement root, ErrorType? type = null);
    }
}