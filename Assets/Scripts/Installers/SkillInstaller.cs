using System;
using Aiming;
using Infrastructure;
using Scriptables;
using Skills;
using UnityEngine;
using VContainer.Unity;

namespace Installers
{
    public class SkillInstaller: IStartable, IDisposable
    {
        private readonly SkillController _skillController;
        private readonly SkillListConfig _skillListConfig;
        private readonly GameLoop _gameLoop;
        private readonly AimingStrategySelector _aimingStrategySelector;

        public SkillInstaller(SkillController skillController, SkillListConfig skillListConfig, GameLoop gameLoop, AimingStrategySelector aimingStrategySelector)
        {
            _skillController = skillController;
            _skillListConfig = skillListConfig;
            _gameLoop = gameLoop;
            _aimingStrategySelector = aimingStrategySelector;
        }

        public void Start()
        {
            _gameLoop.Updated += OnUpdated;
            foreach (var skillConfig in _skillListConfig.GetSkills())
            {
                _skillController.RegisterSkill( skillConfig, new TestSkillExecutor(skillConfig, _aimingStrategySelector));
            }
        }

        private void OnUpdated(float deltaTime)
        {
           _skillController.Tick(deltaTime);
           
        }

        public void Dispose()
        {
            _gameLoop.Updated -= OnUpdated;
        }
    }

    public class TestSkillExecutor : ISkillExecutor
    {
        private readonly SkillConfig _skill;
        private readonly AimingStrategySelector _aimingStrategySelector;

        public TestSkillExecutor(SkillConfig skill, AimingStrategySelector aimingStrategySelector)
        {
            _skill = skill;
            _aimingStrategySelector = aimingStrategySelector;
        }

        public void Execute(Vector2 target)
        {
            Debug.Log($"Skill execute target:{target}");
        }

        public void ExecuteWithCharge(float charge, Vector2 target)
        {
            Debug.Log($"Skill execute charge:{charge} target:{target}");
        }

        public void OnToggleOn()
        {
            Debug.Log($"Skill toggle on");
        }

        public void OnToggleOff()
        {
            Debug.Log($"Skill toggle off");
        }

        public void OnAimStart(Vector2 position)
        {
            Debug.Log($"Skill aim start position:{position}");
            _aimingStrategySelector.StartAiming(position, _skill.AimParameters.Distance, _skill.AimParameters.AimType);
        }

        public void OnAimUpdate(Vector2 position)
        {
           // Debug.Log($"Skill aim update position:{position}");
            _aimingStrategySelector.DirectionAiming(position, _skill.AimParameters.Distance);
        }

        public void OnAimComplete(Vector2 position)
        {
            Debug.Log($"Skill aim complete position:{position}");
            _aimingStrategySelector.EndAiming(_skill.AimParameters.AimType);
        }

        public void OnAimCancel()
        {
            Debug.Log($"Skill aim cancel");
        }

        public void OnChargeStart(float charge)
        {
            Debug.Log($"Skill charge start:{charge}");
        }

        public void OnChargeUpdate(float charge)
        {
            Debug.Log($"Skill charge update:{charge}");
        }

        public void OnChargeComplete(float charge)
        {
            Debug.Log($"Skill charge complete:{charge}");
        }

        public void OnChargeCancel()
        {
            Debug.Log($"Skill charge cancel");
        }

        public void OnCastStart()
        {
            Debug.Log($"Skill cast start");
        }

        public void OnCastUpdate(float progress)
        {
            Debug.Log($"Skill cast progress: {progress:P0}");
        }

        public void OnCastComplete()
        {
            Debug.Log($"Skill cast complete");
        }

        public void OnCastCancel()
        {
            Debug.Log($"Skill cast cancelled");
        }
    }
}