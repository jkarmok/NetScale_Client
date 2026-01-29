using UnityEngine.UIElements;

namespace UI.ViewModels
{
    public class AreaOfInterestViewModel : UIScreen
    {
        private int _activeEntities;
        private Label _activeEntitiesLabel;
        private VisualElement _container;

        public AreaOfInterestViewModel()
        {
        }
        
        public override void Initialize(VisualElement topElement, VisualElement rootElement)
        {
            base.Initialize(topElement,  rootElement);
        }
        
        protected override void SetVisualElements()
        {
            base.SetVisualElements();

            _container = m_TopElement.Q<VisualElement>("entities-stats-container");
            _activeEntitiesLabel = m_TopElement.Q<Label>("active-entities-label");
            _container.pickingMode = PickingMode.Ignore;
        }
        
        public void UpdateStats(int activeEntities)
        {
            _activeEntities = activeEntities;
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (_activeEntitiesLabel != null)
                _activeEntitiesLabel.text = $"Active: {_activeEntities}";
        }
    }
}