using System.Collections;
using UnityEngine;
using ANS = AnimationSystem;

namespace AnimationSystem.Unity.Examples
{
    /// <summary>
    /// Пример: персонаж управляемый сервером с сетевой синхронизацией анимаций.
    ///
    /// Демонстрирует:
    /// 1. Инициализацию AnimatorComponent
    /// 2. Переключение состояний (idle/walk/run) по движению
    /// 3. Foot IK по рейкасту
    /// 4. Сетевую синхронизацию через NetworkAnimatorComponent
    ///
    /// Добавьте на персонажа вместе с AnimatorComponent и NetworkAnimatorComponent.
    /// </summary>
    [RequireComponent(typeof(AnimatorComponent))]
    public sealed class NetworkCharacterExample : MonoBehaviour
    {
        [Header("Movement")]
        public float walkThreshold = 0.5f;
        public float runThreshold = 3f;

        [Header("Clip Names (must match Database)")]
        public string idleClip = "idle";
        public string walkClip = "walk";
        public string runClip = "run";

        [Header("IK")]
        public bool useFootIK = false;
        public string leftFootChain = "LeftFoot";
        public string rightFootChain = "RightFoot";
        public float footRaycastHeight = 0.5f;
        public float footRaycastDistance = 1.5f;
        public LayerMask groundLayer = ~0;


        // ─── Private State ────────────────────────────────────────────────────

        private AnimatorComponent _animator;
        private SimpleCharacterController _rb;
        private string _currentClip = "";
        private float _sendTimer;

        private void Awake()
        {
            _animator = GetComponent<AnimatorComponent>();
            _rb = GetComponent<SimpleCharacterController>();
        }

        private void Start()
        {
            _animator.Initialize();

            if (!string.IsNullOrEmpty(idleClip))
                _animator.Play(idleClip);

        }

        private void Update()
        {
            UpdateLocomotion();
            UpdateFootIK();
        }

        // ─── State Machine ────────────────────────────────────────────────────
        public float currSpeed;
        private void UpdateLocomotion()
        {
            float speed = _rb != null ? _rb.HorizontalSpeed: 0f;

            string targetClip;
            if (speed > runThreshold) targetClip = runClip;
            else if (speed > walkThreshold) targetClip = walkClip;
            else targetClip = idleClip;
            currSpeed = speed;
            
            if (targetClip != _currentClip)
            {
                Debug.Log($"Play anim: {targetClip}");
                _animator.Play(targetClip, 0.2f);
                _currentClip = targetClip;
            }
        }

        // ─── Foot IK ──────────────────────────────────────────────────────────

        private void UpdateFootIK()
        {
            if (!useFootIK || !_animator.IsInitialized) return;

            UpdateSingleFootIK(leftFootChain, -0.1f);
            UpdateSingleFootIK(rightFootChain, 0.1f);
        }

        private void UpdateSingleFootIK(string chainName, float xOffset)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * footRaycastHeight
                + transform.right * xOffset;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, footRaycastDistance, groundLayer))
            {
                _animator.SetIKTarget(chainName, hit.point,
                    Quaternion.FromToRotation(Vector3.up, hit.normal), 0.8f, 0.3f);
            }
            else
            {
                _animator.DisableIK(chainName);
            }
        }
    }
}
