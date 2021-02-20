﻿using System.Collections;
using UnityEngine;

namespace Model.Skills
{
    /// <summary>
    /// 스킬 이름: 베기
    /// </summary>
    public class Skill_000  : Skill
    {
        Extension_000 parsedExtension;
        public Extension_000 ParsedExtension => parsedExtension;
        public Skill_000() : base(0) 
        {
            if (extension != null)
            {
                parsedExtension = Common.Extension.Parse<Extension_000>(extension);
            }
        }
        public override IEnumerator Use(Unit user, Vector2Int target) 
        {
            // 0 단계 : 로그 출력, 스킬 소모 기록, 필요 변수 계산
            user.SkillCount--;
            currentReuseTime = reuseTime;

            //Strength + 강화 횟수 x 2
            int damage = user.Strength * parsedExtension.strengthToDamageRatio + level * parsedExtension.upgradePerEnhancedLevel;

            Unit targetUnit = Managers.BattleManager.GetUnit(target);
            if (targetUnit != null)
            {
                Debug.Log($"{user.Name}가 {name}스킬을 {targetUnit.Name}에 사용!");

                // 1단계 : 스킬 애니메이션 재생 및 화면 갱신.
                user.animationState = Unit.AnimationState.Attack;
                yield return new WaitWhile(() => user.animationState != Unit.AnimationState.Idle);
                targetUnit.animationState = Unit.AnimationState.Hit;

                // 2단계 : 스킬 적용
                Common.UnitAction.Damage(targetUnit, damage);
            }
            else
            {
                Debug.Log($"{user.Name}가 {name}스킬을 {target}에 사용!");
            }
        }
        public override string GetDescription(Unit user, int level)
        {
            int damage = user.Strength * parsedExtension.strengthToDamageRatio + level * parsedExtension.upgradePerEnhancedLevel;
            string str = base.GetDescription(user, level).Replace("X", damage.ToString());
            return str;
        }
        public override void Upgrade()
        {
            if(level < 10)
                base.Upgrade();
        }
    }
    public class Extension_000 : Common.Extensionable
    {
        public int strengthToDamageRatio;
        public int upgradePerEnhancedLevel;
    }
}