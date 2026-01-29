using System;
using System.Collections.Generic;

namespace UI
{
    public sealed class ScreenRegistry
    {
        private readonly Dictionary<ScreenId, UIScreen> _screens = new();

        public void RegisterScreen(ScreenId key, UIScreen uiScreen)
        {
            if (_screens.ContainsKey(key))
            {
                throw new Exception($"Screen with key {key} already exists.");
            }
            _screens[key] = uiScreen;
        }

        public UIScreen Get(ScreenId id)
        {
            if (!_screens.TryGetValue(id, out var uiView))
                throw new KeyNotFoundException($"Screen {id} not registered");

            return uiView;
        }
        
    }
}