using UnityEngine;
using AnimationSystem.Controller;
using AnimationSystem.Unity;
using AnimationSystem.Unity.Loaders;

public class ExampleCharacter : MonoBehaviour
{
    [SerializeField] private AnimationSystemLoader _loader;
    [SerializeField] private AnimatorComponent _animator;

    private AnimationController _controller;

    private void Start()
    {
        if (_loader == null)
            _loader = GetComponent<AnimationSystemLoader>();

        // Загружаем данные
        _loader.Load();
        
        // Создаем контроллер
        _controller = _loader.CreateController();
        
        // Привязываем к аниматору
        if (_animator != null && _controller != null)
        {
            // Здесь нужно связать контроллер с AnimatorComponent
            // В текущей реализации это требует доработки
        }

        // Или загружаем из папки
        // _loader.LoadFromFolder("Assets/AnimationSystem/Export/MyCharacter");
    }

    private void Update()
    {
        // Пример проигрывания анимации
        if (Input.GetKeyDown(KeyCode.Space) && _controller != null)
        {
            _controller.Play("Walk", 0, 0.2f);
        }
    }
}