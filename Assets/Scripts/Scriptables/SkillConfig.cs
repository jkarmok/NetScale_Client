using Aiming;
using UnityEngine;

namespace Scriptables
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Skills/SkillConfig")]
    public class SkillConfig : ScriptableObject
    {
        public string SkillId;
        public string SkillName;
        public SkillType SkillType;
        public SkillActivationType ActivationType;
        public AimParameters AimParameters;
    
        // Базовые параметры
        public float Cooldown = 3f;
        public float ManaCost = 10f;
        public float CastTime = 0f;
    
        // Параметры для разных типов
        public float Damage = 20f;
        public float ProjectileSpeed = 20f;
    
        // Визуальные эффекты
        public GameObject ProjectilePrefab;
        public GameObject ImpactEffect;
 
        public string CastAnimationTrigger;
        public string AimAnimationTrigger;
    }

    public enum SkillType
    {
        Projectile,
        Jump,
        Invisibility,
        AreaAttack,
        Buff,
        Debuff
    }
    [System.Serializable]
    public class AimParameters
    {
        public float Distance;
        public AimType AimType;
        public float AreaRadius;
        public float AreaAngle;
    }
 
    // Типы активации способности
    public enum SkillActivationType
    {
        Instant,        // Мгновенная активация
        Hold,           // Удержание для прицеливания
        Toggle,         // Включение/выключение (например, невидимость)
        Charge          // Зарядка с последующим выстрелом
    }
 
    // Состояния способности
    public enum SkillState
    {
        Ready,
        Aiming,
        Casting,
        Cooldown,
        Active
    }
}