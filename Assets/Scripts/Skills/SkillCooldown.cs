using System;

namespace Skills
{
    public class SkillCooldown
    {
        private readonly float _duration;
        private float _remaining;

        public float Remaining => _remaining;
        public float Duration => _duration;
        public float Progress => _duration > 0f ? 1f - (_remaining / _duration) : 1f;
        public bool IsReady => _remaining <= 0f;

        public event Action OnFinished;
        public event Action<float> OnTicked;

        public SkillCooldown(float duration)
        {
            _duration = duration;
            _remaining = 0f;
        }

        public void Trigger()
        {
            _remaining = _duration;
        }

        public void Reset()
        {
            _remaining = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (_remaining <= 0f) return;

            _remaining -= deltaTime;

            if (_remaining <= 0f)
            {
                _remaining = 0f;
                OnFinished?.Invoke();
            }
            else
            {
                OnTicked?.Invoke(_remaining);
            }
        }
    }
}