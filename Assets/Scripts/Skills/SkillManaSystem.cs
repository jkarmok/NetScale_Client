using System;

namespace Skills
{
    public class SkillManaSystem
    {
        private float _current;
        private float _max;
        private float _regenPerSecond;

        public float Current        => _current;
        public float Max            => _max;
        public float RegenPerSecond => _regenPerSecond;

        public float Normalized => _max > 0f ? _current / _max : 0f;

        public event Action<float, float> OnManaChanged;  // current, max
        public event Action              OnManaExhausted;
        public event Action              OnManaFull;

        public SkillManaSystem(float max, float regenPerSecond = 0f, float initialFill = 1f)
        {
            _max            = max;
            _regenPerSecond = regenPerSecond;
            _current        = max * initialFill;
        }

        public bool HasEnough(float cost) => _current >= cost;

        public bool TrySpend(float cost)
        {
            if (!HasEnough(cost)) return false;
            Set(_current - cost);
            if (_current <= 0f) OnManaExhausted?.Invoke();
            return true;
        }

        public void Restore(float amount) => Set(_current + amount);

        public void RestoreFull() => Set(_max);
        
        public void Tick(float deltaTime)
        {
            if (_regenPerSecond <= 0f) return;
            if (_current >= _max)     return;

            var prev = _current;
            Set(_current + _regenPerSecond * deltaTime);
            if (prev < _max && _current >= _max) OnManaFull?.Invoke();
        }


        public void SetMax(float max, bool keepFull = false)
        {
            _max = max;
            if (keepFull) _current = _max;
            else          _current = Math.Min(_current, _max);
            OnManaChanged?.Invoke(_current, _max);
        }

        public void SetRegen(float regenPerSecond) =>
            _regenPerSecond = regenPerSecond;

        private void Set(float value)
        {
            _current = Math.Clamp(value, 0f, _max);
            OnManaChanged?.Invoke(_current, _max);
        }
    }
}