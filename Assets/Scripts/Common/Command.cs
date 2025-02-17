﻿using UnityEditor;
using UnityEngine;
using Model;
using Model.Managers;
using System.Collections.Generic;
using UI.Battle;
using View;

namespace Common
{
    public static class Command
    {
        public static void Die(this Unit unit)
        {
            AnimationManager.ReserveFadeTextClips(unit, unit.Name + " is dead", Color.red);

            AnimationManager.ReserveAnimationClips("Explosion", unit.Position);
            // View.VisualEffectView.MakeVisualEffect(unit.Position, "Explosion");

            UnSummon(unit);
            // // 소지 유물 뿌리기
            // List<Tile> tiles = FieldManager.GetBlankFloorTiles(unit.Belongings.Count);
            // int count = 0;
            // foreach(Tile tile in tiles)
            // {
            //     Summon(unit.Belongings[count], tile.position);
            //     Vector3 startPos = new Vector3(unit.Position.x, unit.Position.y, 0.1f);
            //     Vector3 target = new Vector3(tile.position.x, tile.position.y, 0.1f);
            //     VisualEffectView.MakeDropEffect(startPos,target,unit.Belongings[count]);
            //     count++;
            // }
            // 사망시 갖고있는 템중에 하나 떨굼
            if (unit.Belongings.Count != 0)
                Summon(unit.Belongings[Random.Range(0, unit.Belongings.Count)], unit.Position);

            //if (BattleManager.CheckGameState() != BattleManager.State.Continue)
        }

        // position 이동보다 좀더 확장된 이동
        // 타일에 유닛의 정보를 기록하고 유닛의 OnMove, 타일의 OnTile 이벤트를 실행시킨다.
        // 이 안에서는 무조건 start Position 사용해야함!!
        // unit.Position은 애니메이션에 사용함.
        public static void Move(this Unit unit, Vector2Int start, Vector2Int target)
        {
            //OnMove는 이동 할때마다 항상 수행되는 이벤트
            target = unit.OnMove.before.Invoke(target);
            FieldManager.GetTile(start).OffTile();

            // 이동한다.
            unit.Position = target;
            unit.OnMove.after.Invoke(target);
            // Model.Tiles.DownStair.CheckPartyDownStair();

            //OnTile은 타일의 특성에 따라 이동이 끝난 후 발동되는 타일의 이벤트
            FieldManager.GetTile(target).OnTile(unit);
        }

        public static int Damage(this Unit unit, int value)
        {
            value = unit.OnDamage.before.Invoke(value);

            unit.CurHP -= value;
            unit.OnDamage.after.Invoke(value);

            AnimationManager.ReserveFadeTextClips(unit, $"HP -{value}", Color.red);

            Debug.Log($"{unit.Name}가(은) {value}만큼 데미지를 입었다! [HP : {unit.CurHP + value}>{unit.CurHP}]");
            if (unit.CurHP <= 0)
                unit.Die();

            return value; // 피해량을 리턴
        }

        public static int Heal(this Unit unit, int value)
        {
            // Debug.Log(unit.Name + ", " + value);

            value = unit.OnHeal.before.Invoke(value);

            // 최대체력 이상으로 회복하지 않습니다.
            if (unit.CurHP + value > unit.MaxHP)
                value = unit.MaxHP - unit.CurHP;

            unit.CurHP += value;
            unit.OnHeal.after.Invoke(value);


            AnimationManager.ReserveFadeTextClips(unit, $"HP +{value}", Color.green);

            Debug.Log($"{unit.Name}가(은) {value}만큼 회복했다! [HP : {unit.CurHP}>{unit.CurHP + value}]");

            return value;
        }

        public static void LevelUp(this Unit unit)
        {
            AnimationManager.ReserveFadeTextClips(unit, "Level Up!", Color.white);

            unit.Level++;

            unit.CurEXP = 0;
            unit.NextEXP = 10 * unit.Level * (unit.Level + 5);
        }

        /// <summary>
        /// 땅에 있는걸 줍는다.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="obtainable"></param>
        public static void AddObtainable(this Unit target, Obtainable obtainable)
        {
            if (obtainable.ToString().Contains("Artifacts"))
            {
                AddArtifact(target, (Artifact)obtainable);
                UnSummon(obtainable);
            }
            else if (obtainable.ToString().Contains("Items"))
            {
                if (target.Alliance == UnitAlliance.Party && GameManager.Instance.itemBag.Count == 3)
                    return;
                else
                {
                    AddItem((Item)obtainable);
                    UnSummon(obtainable);
                }
            }
        }

        /// <summary>
        /// 공용 가방에 아이템을 추가한다.
        /// </summary>
        /// <param name="item"></param>
        public static void AddItem(this Item item)
        {
            if (Data.AllItems.Contains(item))
                item = item.Clone();

            GameManager.Instance.itemBag.Add(item);
            UnitControlView.instance.UpdateItemButtons();
        }


        /// <summary>
        /// 필드랑 관련없이 유물을 얻는다.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="artifact"></param>
        public static void AddArtifact(this Unit target, Artifact artifact)
        {
            if (Data.AllArtifacts.Contains(artifact))
                artifact = artifact.Clone();

            // 아티펙트의 주인은 target이다.
            artifact.Owner = target;

            // 추가 시점 능력 호출
            artifact.OnAdd();

            // 소지품에 추가
            target.Belongings.Add(artifact);

            // 로그 출력
            AnimationManager.ReserveFadeTextClips(target, $"+{artifact.Name}", Color.yellow);
        }

        public static void RemoveArtifact(this Unit target, Artifact artifact)
        {
            if (target.Belongings.Contains(artifact))
            {
                artifact.OnRemove();
                target.Belongings.Remove(artifact);
                AnimationManager.ReserveFadeTextClips(target, $"-{artifact.Name}", Color.yellow);
            }
            else
                Debug.LogError($"{target.Name}이 {artifact.Name}를 소유하고 있지 않습니다.");
        }

        // public static Effect GetEffectByNumber(Unit unit, int value)
        // {
        //     Effect effect = null;

        //     foreach (var stateEffect in unit.StateEffects)
        //         if (stateEffect.Number == value)
        //             effect = stateEffect;

        //     return effect;
        // }

        public static void AddEffect(this Unit target, Effect effect)
        {
            if (Data.AllEffects.Contains(effect))
                effect = effect.Clone();

            Effect oldEffect = null;
            foreach (var stateEffect in target.StateEffects)
                if (effect.GetType() == stateEffect.GetType())
                {
                    oldEffect = stateEffect;
                    break;
                }

            // 새로운 Effect를 추가해준다.
            if (oldEffect == null)
            {
                effect.Owner = target;
                effect.OnAdd();
                target.StateEffects.Add(effect);
                AnimationManager.ReserveFadeTextClips(target, $"+{effect.Name}", Color.yellow);
            }
            // oldEffect가 이미 존재한다면 새로 들어온 new effect는 배제하고 oldEffect.OnOverlap 함수로 처리해준다.
            else
            {
                oldEffect.OnOverlap();
                AnimationManager.ReserveFadeTextClips(target, $"-+{effect.Name}", Color.yellow);
            }
        }

        public static void RemoveEffect(this Unit target, Effect effect)
        {
            if (target.StateEffects.Contains(effect))
            {
                effect.OnRemove();
                target.StateEffects.Remove(effect);
                AnimationManager.ReserveFadeTextClips(target, $"-{effect.Name}", Color.yellow);
            }
            else
                Debug.LogError($"{target.Name}이 {effect.Name}를 소유하고 있지 않습니다.");
        }

        public static void Summon(this Unit unit, Vector2Int position)
        {
            if (BattleManager.GetUnit(position) == null)
            {
                unit.OnPosition.after.RemoveListener(BattleView.MoveObject);
                unit.Position = position;
                // FieldManager.GetTile(position).SetUnit(unit);

                FieldManager.GetTile(position).OnTile(unit);
                BattleManager.instance.AllUnits.Add(unit);
                unit.ActionRate = 0;

                if (unit.Alliance == UnitAlliance.Party)
                    GameManager.AddPartyUnit(unit);

                BattleView.MakeUnitObject(unit);

                // 유닛 소환시 DownStair Button 활성화 검사
                // Model.Tiles.DownStair.CheckPartyDownStair();
            }
            else
                Debug.LogWarning("이미 위치에 유닛이 존재합니다.");
        }

        // 최초 Obatainable 최초 소환
        public static void Summon(this Obtainable obtainable, Vector2Int position)
        {
            if (BattleManager.GetUnit(position) != null)
            {
                AddObtainable(BattleManager.GetUnit(position), obtainable);
            }
            else if (BattleManager.GetObtainable(position) == null)
            {
                FieldManager.GetTile(position).SetObtainable(obtainable);
                obtainable.Position = position;
                BattleManager.instance.AllObtainables.Add(obtainable);
                BattleView.MakeObtainableObject(obtainable, position);
                // Debug.Log(obtainable.ToString());
            }
            else
                Debug.LogWarning("이미 위치에 뭔가 존재합니다.");
        }
        public static void UnSummon(Vector2Int position)
        {
            Tile tile = FieldManager.GetTile(position);
            Obtainable obt = tile.GetObtainable();
            Unit unit = tile.GetUnit();
            if (obt != null) UnSummon(obt);
            if (unit != null) UnSummon(unit);
        }

        public static void UnSummon(this Unit unit)
        {
            if (unit.Alliance == UnitAlliance.Party)
                GameManager.RemovePartyUnit(unit); //죽으면 파티유닛에서 박탈.

            FieldManager.GetTile(unit.Position).OffTile();

            BattleView.DestroyUnitObject(unit);
            BattleManager.instance.AllUnits.Remove(unit);
            BattleManager.instance.InitializeUnitBuffer();

            // 유닛 소환해제시 DownStair Button 활성화 검사
            // Model.Tiles.DownStair.CheckPartyDownStair();

            //적 전멸 -> (승리조건 검사 목적), 내가 죽음 -> (턴을 다음 유닛으로 넘길 목적)
            if (GameManager.InBattle)
            {
                if (BattleManager.GetUnit(UnitAlliance.Enemy).Count == 0)
                    BattleController.instance.ThisTurnEnd();

                else if (BattleManager.instance.thisTurnUnit == unit)
                {
                    BattleManager.instance.thisTurnUnit = null;
                    BattleController.instance.ThisTurnEnd();
                }
            }

            //비전투시 유닛 표시기 비활성화
            else if (BattleManager.instance.thisTurnUnit == unit)
                BattleManager.instance.thisTurnUnit = null;
        }


        public static void UnSummon(this Obtainable obtainable)
        {
            if (obtainable.Position == null) return;
            FieldManager.GetTile((Vector2Int)obtainable.Position).SetObtainable(null);
            BattleView.DestroyObtainableObject(obtainable);
            BattleManager.instance.AllObtainables.Remove(obtainable);
        }

        public static void UnSummonAllUnit()
        {
            List<Unit> units = BattleManager.instance.AllUnits;
            for (int i = units.Count - 1; i >= 0; i--)
            {
                Unit unit = units[i];
                if (unit.Alliance == UnitAlliance.Party)
                    GameManager.RemovePartyUnit(unit); //죽으면 파티유닛에서 박탈.
                BattleView.DestroyUnitObject(unit);
                BattleManager.instance.AllUnits.Remove(unit);
                FieldManager.GetTile(unit.Position).OffTile();
            }
            BattleManager.instance.AllUnits.Clear();
        }

        public static void UnSummonAllObtainable()
        {
            List<Obtainable> obtainables = BattleManager.instance.AllObtainables;
            for (int i = obtainables.Count - 1; i >= 0; i--)
            {
                UnSummon(obtainables[i]);
            }
            BattleManager.instance.AllObtainables.Clear();
        }

        public static void AddSkill(this Unit unit, Skill newSkill, int level = 0)
        {
            if (Data.AllSkills.Contains(newSkill))
                newSkill = newSkill.Clone(level);

            UpgradeSkill(newSkill, level);

            if (newSkill is Model.Skills.Move.MoveSkill)
            {
                RemoveSkill(unit, unit.MoveSkill);
                unit.MoveSkill = newSkill as Model.Skills.Move.MoveSkill;
                newSkill.User = unit;

                // Debug.Log($"{unit.Name} {newSkill.GetType()} {newSkill.User}");
            }
            else if (unit.Skills.Count < 3 && !unit.Skills.Contains(newSkill))
            {
                unit.Skills.Add(newSkill);
                newSkill.User = unit;
            }
            else
                Debug.LogError($"{newSkill.Name} 스킬을 배울 수 없습니다.");
        }

        /// <summary>
        /// 스킬의 레벨을 1 올린다.
        /// </summary>
        /// <param name="skill"></param>
        public static void UpgradeSkill(this Skill skill)
        {
            // 레벨 적용은 안에서 호출해줌
            skill.OnUpgrade(skill.Level + 1);
        }

        /// <summary>
        /// 스킬의 레벨을 level로 올린다.
        /// </summary>
        /// <param name="skill"></param>
        /// <param name="level"></param>
        public static void UpgradeSkill(this Skill skill, int level)
        {
            // 레벨 적용은 안에서 호출해줌
            skill.OnUpgrade(level);
        }

        public static void RemoveSkill(this Unit unit, Skill skill)
        {
            // 대기중인 스킬에 존재한다면, 미리 삭제해준다.
            // if (unit.WaitingSkills.ContainsKey(skill))
            //     unit.WaitingSkills.Remove(skill);

            // 강화한 스킬 리스트에 존재한다면, 삭제해줍니다.
            // if (unit.EnhancedSkills.ContainsKey(skill))
            //     unit.EnhancedSkills.Remove(skill);

            // 스킬 삭제
            if (unit.MoveSkill == skill)
            {
                unit.MoveSkill = new Model.Skills.Move.MoveSkill();
            }
            else if (unit.Skills.Contains(skill))
            {
                unit.Skills.Remove(skill);
            }
            else
                Debug.LogError($"{skill.Name}현재 소유한 스킬이 아닙니다. 삭제할 수 없습니다.");
        }

        // public static void UpgradeSkill(Unit unit, int index)
        // {
        //     if (index >= unit.Skills.Length || index < 0)
        //     {
        //         Debug.LogError("스킬 슬롯을 범위를 벗어났습니다.");
        //         return;
        //     }
        //     if (unit.Skills[index] == null)
        //     {
        //         Debug.LogError("업그레이드 할 스킬이 없습니다.");
        //         return;
        //     }
        //     unit.Skills[index].Upgrade();
        // }

    }
}