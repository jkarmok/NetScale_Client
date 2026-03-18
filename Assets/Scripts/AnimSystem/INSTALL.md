# AnimationSystem Unity Integration — Руководство по установке

## Структура пакета

```
Assets/
└── AnimationSystem/
    ├── AnimationSystem.dll              ← скомпилированная библиотека (.NET Standard 2.1)
    └── UnityIntegration/
        ├── package.json
        ├── Runtime/
        │   ├── AnimationSystem.Unity.Runtime.asmdef
        │   ├── Assets/
        │   │   ├── SkeletonAsset.cs
        │   │   ├── AnimationClipAsset.cs
        │   │   └── AnimationDatabaseAsset.cs
        │   ├── Components/
        │   │   ├── AnimatorComponent.cs
        │   │   └── NetworkAnimatorComponent.cs
        │   ├── Conversion/
        │   │   └── UnityClipConverter.cs
        │   └── Examples/
        │       └── NetworkCharacterExample.cs
        └── Editor/
            ├── AnimationSystem.Unity.Editor.asmdef
            ├── Windows/
            │   ├── SkeletonExporterWindow.cs
            │   ├── ClipExporterWindow.cs
            │   └── AnimationSystemDebugWindow.cs
            └── Inspectors/
                ├── AnimatorComponentInspector.cs
                ├── SkeletonAssetInspector.cs
                ├── AnimationClipAssetInspector.cs
                └── AnimationDatabaseAssetInspector.cs
```

---

## Шаг 1 — Установка DLL

1. Соберите `AnimationSystem.dll` из проекта `AnimationSystem.csproj` (target: `netstandard2.1`):
   ```
   dotnet build -c Release
   ```
2. Скопируйте `AnimationSystem.dll` в `Assets/Plugins/AnimationSystem.dll`

---

## Шаг 2 — Создание SkeletonAsset

### Автоматически (рекомендуется)

1. Откройте **AnimationSystem → Skeleton Exporter**
2. Перетащите корень персонажа в поле **Character Root**
3. Нажмите **Scan Hierarchy**
4. Выберите нужные кости (кнопка "Select Humanoid" для стандартного риггинга)
5. Нажмите **Export Skeleton Asset**

### Вручную

1. `ПКМ в Project → Create → AnimationSystem → Skeleton Asset`
2. Заполните массив `Bones`:
   - `boneIndex` — порядковый номер (родитель всегда < дочерней кости)
   - `boneName` — уникальное имя (совпадает с именем GameObject)
   - `parentIndex` — индекс родительской кости (-1 для корня)
   - `transformPath` — путь от корня, например `Hips/Spine/Chest`
   - `bindLocalPosition/Rotation/Scale` — Transform в T-позе

---

## Шаг 3 — Создание AnimationClipAsset

### Через Clip Exporter (из Unity AnimationClip)

1. Откройте **AnimationSystem → Clip Exporter**
2. Назначьте **Skeleton Asset**
3. При необходимости укажите **Reference Character** (нужен для Sampling mode)
4. Перетащите Unity AnimationClip'ы в Drop Zone или нажмите "+ From Selection"
5. Назначьте имена, ID и флаг Loop каждому клипу
6. Нажмите **Convert X Clip(s)**

### Вручную

1. `ПКМ → Create → AnimationSystem → Clip Asset`
2. Заполните `clipId`, `clipName`, `duration`, `isLooping`
3. Добавьте треки `tracks[]`:
   - `boneIndex` — индекс кости
   - `positionKeys`, `rotationKeys`, `scaleKeys` — ключевые кадры
   - Каждый ключ: `time`, `value`, `inTangent`/`outTangent` (для Hermite)

---

## Шаг 4 — Создание AnimationDatabaseAsset

1. `ПКМ → Create → AnimationSystem → Database Asset`
2. Перетащите все AnimationClipAsset в инспекторе
3. **Или** нажмите **"Find All Clips"** в инспекторе — автоматически найдёт все клипы в проекте

---

## Шаг 5 — Настройка персонажа

1. Добавьте компонент **AnimationSystem → Animator Component** на корень персонажа
2. Назначьте `SkeletonAsset` и `AnimationDatabaseAsset`
3. Опционально: укажите `Default Clip` — имя клипа для старта
4. Опционально: настройте **Layer Configs** для многослойного смешивания

### Многослойное смешивание

```
Layer 0 (Override, weight=1.0, all bones): "walk"
Layer 1 (Override, weight=0.8, upper body only): "reload"
```

В Layer Configs:
- Layer 0: `affectsAllBones=true`
- Layer 1: `affectsAllBones=false`, `boneNames=[Spine, Chest, LeftShoulder, ...]`

---

## Шаг 6 — Настройка IK (опционально)

В инспекторе **SkeletonAsset** добавьте IK Chains:

| Поле | Описание |
|------|----------|
| `chainName` | Идентификатор (напр. `LeftFoot`) |
| `chainType` | `TwoBone` (нога/рука) или `FABRIK` (произвольная цепочка) |
| `boneIndices` | Индексы костей: [Thigh, Knee, Ankle] |

Из кода:
```csharp
animator.SetIKTarget("LeftFoot", hitPoint, hitNormal, posWeight: 0.9f);
animator.DisableIK("LeftFoot");
```

---

## Шаг 7 — Сетевая синхронизация

### Сервер
```csharp
// Создание
var simulator = new ServerAnimationSimulator(skeletonDef, tickRate: 30f, database: db);
simulator.OnPacketReady += bytes => SendToAllClients(bytes);
simulator.Controller.Play("idle");

// В fixed update
simulator.Tick();

// Смена анимации
simulator.Controller.Play("walk", 0, transitionDuration: 0.3f);
```

### Клиент
```csharp
// Добавьте NetworkAnimatorComponent рядом с AnimatorComponent
// Настройте extrapolate = true

// При получении пакета из сети:
networkAnimator.OnPacketReceived(rawBytes);
```

---

## Меню Editor

| Пункт меню | Назначение |
|------------|-----------|
| AnimationSystem → Skeleton Exporter | Создание SkeletonAsset из иерархии |
| AnimationSystem → Clip Exporter | Конвертация Unity AnimationClip |
| AnimationSystem → Debug Window | Отладка, просмотр состояний и поз |

---

## Известные ограничения

- **Quaternion IK**: RotationWeight в IK работает, но поворот конечного эффектора нужно
  настраивать вручную после смены rig-а.
- **Scale анимации**: поддерживается, но аддитивное смешивание масштаба может давать нелинейные результаты.
- **Sampling-конвертер** требует референсный GameObject в сцене.
- **AnimationDatabase.Instance** — глобальный синглтон; если нужны несколько независимых баз (разные персонажи с одинаковыми ID клипов) — создайте отдельные экземпляры `new AnimationDatabase()` и передавайте их в конструктор `AnimationController`.
