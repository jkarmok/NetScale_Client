using System;
using System.Collections.Generic;
using Aiming;
using Game.Skills;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Scriptables.DTO
{
    public static class SkillDTOMapper
    {
        public static SkillListDTO ToDTO(SkillListConfig skillList)
        {
            if (skillList == null) return null;

            var dto = new SkillListDTO
            {
                ListId = skillList.ListId,
                ListName = skillList.ListName,
                TotalSkills = skillList.Count,
                Skills = new List<SkillDTO>()
            };

            for (int i = 0; i < skillList.Count; i++)
            {
                var skill = skillList.GetSkillByIndex(i);
                if (skill != null)
                {
                    dto.Skills.Add(ToDTO(skill));
                }
            }

            return dto;
        }

        public static SkillDTO ToDTO(SkillConfig skill)
        {
            if (skill == null) return null;

            var dto = new SkillDTO
            {
                SkillId = skill.SkillId,
                SkillName = skill.SkillName,
                SkillType = skill.SkillType.ToString(),
                ActivationType = skill.ActivationType.ToString(),
                Cooldown = skill.Cooldown,
                ManaCost = skill.ManaCost,
                CastTime = skill.CastTime,
                Damage = skill.Damage,
                ProjectileSpeed = skill.ProjectileSpeed,
                CastAnimationTrigger = skill.CastAnimationTrigger,
                AimAnimationTrigger = skill.AimAnimationTrigger
            };

            // Конвертируем AimParameters
            if (skill.AimParameters != null)
            {
                dto.AimParameters = new AimParametersDTO
                {
                    Distance = skill.AimParameters.Distance,
                    AimType = skill.AimParameters.AimType.ToString(),
                    AreaRadius = skill.AimParameters.AreaRadius,
                    AreaAngle = skill.AimParameters.AreaAngle
                };
            }

            // Сохраняем пути к префабам вместо прямых ссылок
#if UNITY_EDITOR
            if (skill.ProjectilePrefab != null)
            {
                dto.ProjectilePrefabPath = AssetDatabase.GetAssetPath(skill.ProjectilePrefab);
            }

            if (skill.ImpactEffect != null)
            {
                dto.ImpactEffectPath = AssetDatabase.GetAssetPath(skill.ImpactEffect);
            }
#endif

            return dto;
        }

        // Обратная конвертация (если понадобится загружать с сервера)
        public static SkillConfig FromDTO(SkillDTO dto)
        {
            if (dto == null) return null;

            var skill = ScriptableObject.CreateInstance<SkillConfig>();
            
            skill.SkillId = dto.SkillId;
            skill.SkillName = dto.SkillName;
            
            if (Enum.TryParse(dto.SkillType, out SkillType skillType))
                skill.SkillType = skillType;
            
            if (Enum.TryParse(dto.ActivationType, out SkillActivationType activationType))
                skill.ActivationType = activationType;
            
            skill.Cooldown = dto.Cooldown;
            skill.ManaCost = dto.ManaCost;
            skill.CastTime = dto.CastTime;
            skill.Damage = dto.Damage;
            skill.ProjectileSpeed = dto.ProjectileSpeed;
            skill.CastAnimationTrigger = dto.CastAnimationTrigger;
            skill.AimAnimationTrigger = dto.AimAnimationTrigger;

            if (dto.AimParameters != null)
            {
                skill.AimParameters = new AimParameters
                {
                    Distance = dto.AimParameters.Distance,
                    AreaRadius = dto.AimParameters.AreaRadius,
                    AreaAngle = dto.AimParameters.AreaAngle
                };

                if (Enum.TryParse(dto.AimParameters.AimType, out AimType aimType))
                {
                    skill.AimParameters.AimType = aimType;
                }
            }

            // Здесь можно добавить загрузку префабов по путям, если нужно
            // skill.ProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(dto.ProjectilePrefabPath);

            return skill;
        }
    }
}