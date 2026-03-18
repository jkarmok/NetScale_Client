using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AnimationSystem.Skeleton;
using AnimationSystem.Animation;
using AnimationSystem.Controller;
using AnimationSystem.Serialization;
using AnimationClip = AnimationSystem.Animation.AnimationClip;

namespace AnimationSystem.Unity.Loaders
{
    /// <summary>
    /// Загрузчик анимационных данных из экспортированных файлов.
    /// Использует AnimationSystem.Loaders для загрузки скелета и анимаций.
    /// </summary>
    public sealed class AnimationSystemLoader : MonoBehaviour
    {
        [Header("Load Settings")]
        [SerializeField] private string _characterName = "Character";
        [SerializeField] private TextAsset _skeletonJson;
        [SerializeField] private TextAsset[] _animationClips;
        [SerializeField] private bool _loadOnAwake = true;
        
        [Header("Runtime")]
        [SerializeField] private AnimatorComponent _animator;
        
        private SkeletonDefinition _loadedSkeleton;
        private AnimationDatabase _database;
        private AnimationController _controller;

        public SkeletonDefinition LoadedSkeleton => _loadedSkeleton;
        public AnimationDatabase Database => _database;
        public AnimationController Controller => _controller;

        private void Awake()
        {
            if (_loadOnAwake)
                Load();
        }

        /// <summary>
        /// Загружает скелет и анимации из назначенных ассетов.
        /// </summary>
        public void Load()
        {
            if (_animator == null)
                _animator = GetComponent<AnimatorComponent>();

            LoadSkeleton();
            LoadAnimations();

            if (_animator != null && _loadedSkeleton != null)
            {
                // Автоматически инициализируем аниматор если нужно
                if (!_animator.IsInitialized)
                {
                    // В реальном проекте здесь нужно создать SkeletonAsset
                    Debug.LogWarning($"[Loader] AnimatorComponent not initialized. Manual setup required.");
                }
            }
        }

        /// <summary>
        /// Загружает скелет из JSON.
        /// </summary>
        public SkeletonDefinition LoadSkeleton()
        {
            if (_skeletonJson == null)
            {
                Debug.LogError($"[Loader] Skeleton JSON not assigned!");
                return null;
            }

            try
            {
                _loadedSkeleton = SkeletonLoader.LoadFromJson(_skeletonJson.text);
                Debug.Log($"[Loader] Skeleton loaded: {_loadedSkeleton.BoneCount} bones");
                return _loadedSkeleton;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Loader] Failed to load skeleton: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Загружает анимации из бинарных файлов.
        /// </summary>
        public AnimationDatabase LoadAnimations()
        {
            if (_animationClips == null || _animationClips.Length == 0)
            {
                Debug.LogWarning($"[Loader] No animation clips assigned");
                return null;
            }

            if (_loadedSkeleton == null)
            {
                Debug.LogError($"[Loader] Skeleton must be loaded before animations!");
                return null;
            }

            _database = new AnimationDatabase();
            _database.SetSkeleton(_loadedSkeleton);

            int loaded = 0;
            foreach (var clipAsset in _animationClips)
            {
                if (clipAsset == null) continue;

                try
                {
                    var clip = AnimationClipLoader.LoadFromBinary(
                        clipAsset.bytes, 
                        _loadedSkeleton.BoneCount);
                    
                    _database.Register(clip);
                    loaded++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Loader] Failed to load clip {clipAsset.name}: {e.Message}");
                }
            }

            Debug.Log($"[Loader] Loaded {loaded}/{_animationClips.Length} animations");
            return _database;
        }

        /// <summary>
        /// Создает контроллер для использования в рантайме.
        /// </summary>
        public AnimationController CreateController()
        {
            if (_loadedSkeleton == null)
            {
                Debug.LogError($"[Loader] Cannot create controller: skeleton not loaded");
                return null;
            }

            _controller = new AnimationController(_loadedSkeleton, _database);
            return _controller;
        }

        /// <summary>
        /// Загружает все данные из указанной папки.
        /// </summary>
        public void LoadFromFolder(string folderPath)
        {
            // Загрузка скелета
            string skeletonPath = Path.Combine(folderPath, $"{_characterName}_skeleton.json");
            if (File.Exists(skeletonPath))
            {
                var json = File.ReadAllText(skeletonPath);
                _loadedSkeleton = SkeletonLoader.LoadFromJson(json);
            }

            // Загрузка анимаций
            string animsPath = Path.Combine(folderPath, "Animations");
            if (Directory.Exists(animsPath))
            {
                var animFiles = Directory.GetFiles(animsPath, "*.anim");
                var clips = new List<AnimationClip>();
                
                foreach (var file in animFiles)
                {
                    var data = File.ReadAllBytes(file);
                    var clip = AnimationClipLoader.LoadFromBinary(data, _loadedSkeleton.BoneCount);
                    clips.Add(clip);
                }

                _database = new AnimationDatabase();
                _database.SetSkeleton(_loadedSkeleton);
                foreach (var clip in clips)
                    _database.Register(clip);
            }
        }
    }
}