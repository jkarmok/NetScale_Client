using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif
[UxmlElement]
public partial class SpriteSheetAnimatorElement : VisualElement
{
    [UxmlAttribute] 
    public float FrameRate { get; set; } = 0.1f;

    [UxmlAttribute("animation-frames")] 
    public Sprite[] AnimationFrames { get; set; }

    public Action FrameChanged;
    public Action AnimationCompleted;
    
    private int currentFrame;
    private float timer;
    private bool isAnimating;

    public new class UxmlFactory : UxmlFactory<SpriteSheetAnimatorElement, UxmlTraits>
    {
    }

    public SpriteSheetAnimatorElement()
    {
        RegisterCallback<AttachToPanelEvent>(OnAttached);
        RegisterCallback<DetachFromPanelEvent>(OnDetached);
    }


    private void OnAttached(AttachToPanelEvent evt)
    {
        isAnimating = true;
        timer = 0f;
        currentFrame = 0;
        
 
        if (AnimationFrames != null && AnimationFrames.Length > 0)
        {
            style.backgroundImage = new StyleBackground(AnimationFrames[currentFrame]);
        }

        schedule.Execute(UpdateAnimation).Every((long)(FrameRate * 1000)).Until(() => !isAnimating);
    }

    private void OnDetached(DetachFromPanelEvent evt)
    {
        // Остановка анимации
        isAnimating = false;
    }

    private void UpdateAnimation()
    {

        if (AnimationFrames == null || AnimationFrames.Length == 0) return;

        timer += FrameRate;
        currentFrame = (currentFrame + 1);
        if (currentFrame == AnimationFrames.Length)
        {
            AnimationCompleted?.Invoke();
        }
        currentFrame %= AnimationFrames.Length;
 
        style.backgroundImage = new StyleBackground(AnimationFrames[currentFrame]);
        FrameChanged?.Invoke();
 
    }
}

#if UNITY_EDITOR
public class SpriteArrayAttributeConverter : UxmlAttributeConverter<Sprite[]>
{
    public override Sprite[] FromString(string value)
    {
        if (string.IsNullOrEmpty(value)) return new Sprite[0];

        // Парсим: "path/to/atlas.png|sprite1,sprite2"
        var parts = value.Split('|');
        if (parts.Length < 2) return new Sprite[0];

        string atlasPath = parts[0];
        string[] spriteNames = parts[1].Split(',');

        // Загружаем атлас
        Texture2D atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
        if (atlasTexture == null) return new Sprite[0];

        // Получаем спрайты из атласа
        Sprite[] allSprites = AssetDatabase.LoadAllAssetsAtPath(atlasPath).OfType<Sprite>().ToArray();
        return spriteNames
            .Select(name => allSprites.FirstOrDefault(sprite => sprite.name == name))
            .Where(sprite => sprite != null)
            .ToArray();
    }

    public override string ToString(Sprite[] value)
    {
        if (value == null || value.Length == 0) return string.Empty;

        // Получаем путь к атласу
        string atlasPath = AssetDatabase.GetAssetPath(value[0].texture);
        if (string.IsNullOrEmpty(atlasPath)) return string.Empty;

        // Собираем имена спрайтов
        string spriteNames = string.Join(",", value.Select(sprite => sprite.name));
        return $"{atlasPath}|{spriteNames}";
    }
}
#endif