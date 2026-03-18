using System.Collections.Generic;
using Scriptables;
using UnityEngine;

namespace Skills
{
    public class SkillController
    {
        private readonly SkillActivatorRegistry _registry;
        private readonly SkillManaSystem _mana;

        private readonly Dictionary<string, SkillMeta> _meta = new();
        public SkillActivatorRegistry Registry => _registry;
        public SkillManaSystem Mana => _mana;

        public SkillController(SkillManaSystem mana, SkillActivatorRegistry registry)
        {
            _mana = mana;
            _registry = registry;
        }

        public void RegisterSkill(SkillConfig config, ISkillExecutor executor)
        {
         
            var skillCooldown = new  SkillCooldown(config.Cooldown);
            var activator = new Activator(config.ActivationType, () => skillCooldown.IsReady && _mana.HasEnough(config.ManaCost), config.CastTime);
            SkillActivatorBinder.Bind(activator, executor);
 
            activator.OnCastExecuted += () =>
            {
                _mana.TrySpend(config.ManaCost);
                skillCooldown.Trigger();
            };

            _registry.Register(config.SkillId, activator);
            _meta[config.SkillId] = new SkillMeta(config, activator, skillCooldown);
        }

        public void Tick(float deltaTime)
        {
            foreach (var skillMeta in _meta)
            {
                skillMeta.Value.Activator.Tick(deltaTime);
                skillMeta.Value.SkillCooldown.Tick(deltaTime);
            }
            _mana.Tick(deltaTime);
        }
 
        public SkillUISnapshot GetSnapshot(string skillId)
        {
            _meta.TryGetValue(skillId, out var meta);

            return new SkillUISnapshot(
                SkillId: skillId,
                State: meta.Activator?.CurrentState ?? SkillState.Ready,
                CooldownRemaining: meta.SkillCooldown.Remaining,
                CooldownProgress: meta.SkillCooldown.Progress,
                ManaCurrent: _mana.Current,
                ManaMax: _mana.Max,
                CanActivate: meta.SkillCooldown.IsReady && _mana.HasEnough(meta.Config.ManaCost)
            );
        }

        private record SkillMeta(SkillConfig Config, Activator Activator, SkillCooldown SkillCooldown)
        {
            private readonly SkillConfig config1 = Config;
            private readonly Activator activator1 = Activator;
            private readonly SkillCooldown skillCooldown1 = SkillCooldown;

            public SkillConfig Config => config1;

            public Activator Activator => activator1;

            public SkillCooldown SkillCooldown => skillCooldown1;
        }
    }

    public record SkillUISnapshot(
        string SkillId,
        SkillState State,
        float CooldownRemaining,
        float CooldownProgress, // [0..1], 1 = готово
        float ManaCurrent,
        float ManaMax,
        bool CanActivate
    )
    {
        private readonly string skillId = SkillId;
        private readonly float cooldownRemaining = CooldownRemaining;
        private readonly SkillState state = State;
        private readonly float cooldownProgress = CooldownProgress;
        private readonly float manaCurrent = ManaCurrent;
        private readonly float manaMax = ManaMax;
        private readonly bool canActivate = CanActivate;

        public string SkillId => skillId;

        public float CooldownRemaining => cooldownRemaining;

        public SkillState State => state;

        public float CooldownProgress => cooldownProgress;

        public float ManaCurrent => manaCurrent;

        public float ManaMax => manaMax;

        public bool CanActivate => canActivate;
    }
    public interface ISkillExecutor
    {
        void Execute(Vector2 target);
        void ExecuteWithCharge(float charge, Vector2 target);
        void OnToggleOn();
        void OnToggleOff();
        void OnAimStart(Vector2 position);
        void OnAimUpdate(Vector2 position);
        void OnAimComplete(Vector2 position);
        void OnAimCancel();
        void OnChargeStart(float charge);
        void OnChargeUpdate(float charge);
        void OnChargeComplete(float charge);
        void OnChargeCancel();
        void OnCastStart();
        void OnCastUpdate(float progress); // [0..1]
        void OnCastComplete();
        void OnCastCancel();
    }

}