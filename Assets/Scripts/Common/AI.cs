﻿using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Model;
using UI.Battle;
using View;
using Model.Managers;

namespace Common
{
    public class AI
    {
        /// <summary>
        /// 행동의 우선순위
        /// </summary>
        public enum Priority
        {
            NULL,                   // 우선순위 없음

            NearFromMe,             // 나에게 가까운 곳을 우선으로 사용
            FarFromMe,              // 나에게 먼곳을 우선으로 사용

            BiggerCurHP,            // 현재HP가 높은 유닛을 우선으로 사용
            SmallerCurHP,           // 현재HP가 낮은 유닛을 우선으로 사용

            NearFromPartys,         // 파티들에게 가까운 곳을 우선으로 사용
            FarFromPartys,          // 파티들에게 먼곳을 우선으로 사용

            NearFromClosestParty,   // 가장 가까운 파티원에게서 가까운 곳을 우선으로 사용
            FarFromClosestParty,    // 가장 가까운 파티원에게서 먼 곳을 우선으로 사용
        }

        public class Action
        {
            public Unit user;                   // 행동 주체
            public Skill skillToUse;            // 사용할 스킬
            public Vector2Int? targetPosition;   // 스킬을 사용할 위치
            public Vector2Int? movePosition;     // 이동할 위치

            public Action(Unit user, Skill skillToUse, Vector2Int? targetPosition, Vector2Int? movePosition)
            {
                this.user = user;
                this.skillToUse = skillToUse;
                this.targetPosition = targetPosition;
                this.movePosition = movePosition;
            }

            public void Invoke()
            {
                if (BattleController.action_coroutine != null)
                {
                    Debug.LogError("action 코루틴 이미 실행중!!");
                    return;
                }

                BattleController.action_coroutine = BattleController.instance.StartCoroutine(InvokeAIAction(this));
            }

            public IEnumerator InvokeAIAction(Action action)
            {
                BattleView.TurnEndButton.interactable = false;

                yield return new WaitForSeconds(GameManager.AnimationDelaySpeed);

                Debug.Log("MoveSkill");
                // 이동할 위치가 있으면
                if (action.movePosition != null)
                    yield return BattleController.instance.StartCoroutine(action.user.MoveSkill.Use((Vector2Int)action.movePosition));

                yield return new WaitForSeconds(GameManager.AnimationDelaySpeed);

                Debug.Log("skillToUse");
                // 사용할 스킬이 있고, 사용할 위치가 존재한다면 사용
                if (action.skillToUse != null && action.targetPosition != null)
                {
                    yield return BattleController.instance.StartCoroutine(action.skillToUse.Use((Vector2Int)action.targetPosition));
                    yield return new WaitForSeconds(GameManager.AnimationDelaySpeed);
                }

                BattleView.TurnEndButton.interactable = true;
                BattleController.action_coroutine = null;

                if (BattleManager.CheckGameState() == BattleManager.State.Continue)
                    AnimationManager.OnAnimationComplete(() => BattleController.instance.ThisTurnEnd());
            }
        }

        /// <summary>
        /// AI 발동
        /// </summary>
        /// <param name="user"></param>
        public static Action GetAction(Unit user)
        {
            // 1. 사용할 스킬을 정한다.
            // 1.1. 사용할수 있는가?
            // 1.2. 스킬을 사용할수 있어야함 : 재사용 대기시간이 0이어야하고, 스킬 행동력이 있어야함
            // 1.3. 이동 + 스킬범위 내에 사용할수 있는 유닛이 있어야함)
            // 2. 스킬을 사용할 위치를 정한다. (사용스킬 우선순위에 따름)
            // 3. 스킬을 사용할 수 있는 위치를 역계산한다. (이동스킬 우선순위에 따름)
            // 4. 역계산한 스킬위치들 중에서 하나를 선택한다.

            Skill skillToUse = null;

            int reuseTime = 0;
            foreach (var skill in user.Skills)
            {
                if (skill == null)
                    continue;

                // 재사용 대기시간이 가장 긴, 사용할수 있는 스킬을 찾는다.
                if (skill.IsUsable()
                && skill.ReuseTime >= reuseTime
                && GetTargetPosition(user, skill) != null)
                {
                    skillToUse = skill;
                    reuseTime = skill.ReuseTime;
                }
            }

            // 사용할수 있는 스킬이 있는 경우
            if (skillToUse != null)
            {
                Vector2Int targetPosition = (Vector2Int)GetTargetPosition(user, skillToUse);
                Vector2Int movePosiiton = (Vector2Int)GetMovePosition(user, skillToUse, targetPosition);
                return new Action(user, skillToUse, targetPosition, movePosiiton);
            }
            // 사용할수 있는 스킬이 없는 경우
            else
            {
                return new Action(user, null, null, GetMovePosition(user));
            }
        }

        /// <summary>
        /// 이동과 같이 스킬을 사용할수 있는 총 범위
        /// </summary>
        /// <param name="user"></param>
        /// <param name="skill"></param>
        /// <returns></returns>
        private static List<Vector2Int> GetMovedSkillablePositions(Unit user, Skill skill)
        {
            // 이동후 스킬 사용한 모든 위치
            List<Vector2Int> MovedSkillablePositions = new List<Vector2Int>();

            // 이동 가능한 위치
            List<Vector2Int> MoveablePositions = new List<Vector2Int>();
            MoveablePositions.Add(user.Position);
            MoveablePositions.AddRange(user.MoveSkill.GetAvlUsePositions());

            foreach (var moveablePosition in MoveablePositions)
            {
                // 이동 후 그 위치에서 스킬이 가능한 위치 리스트를 찾는다.
                List<Vector2Int> SkillablePositions = skill.GetAvlPositions(moveablePosition);

                // 스킬을 사용가능한 위치를 순회하면서
                foreach (var skillablePosition in SkillablePositions)
                {
                    // 리스트에 아직 추가 안된 위치라면 추가한다.
                    if (!MovedSkillablePositions.Contains(skillablePosition))
                        MovedSkillablePositions.Add(skillablePosition);
                }
            }

            return MovedSkillablePositions;
        }

        /// <summary>
        /// 타겟에 스킬을 사용할 수 있는 (이동가능한)위치들을 반환한다.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        private static List<Vector2Int> GetTargetedMoveablePositions(Unit user, Skill skill, Vector2Int targetPosition)
        {
            // 정해져 있는 타겟에 스킬을 사용할수 있는 이동가능한 위치들
            List<Vector2Int> TargetedMovablePositions = new List<Vector2Int>();

            // 이동가능한 모든 위치들
            List<Vector2Int> MoveablePositions = user.MoveSkill.GetAvlUsePositions();
            // 현재위치도 일단 추가;;
            MoveablePositions.Add(user.Position);

            // 이동가능한 위치들을 순회하면서
            foreach (var moveablePosition in MoveablePositions)
            {
                // 이 위치에서 스킬을 사용가능한 위치들을 찾는다.
                List<Vector2Int> SkillablePositions = skill.GetAvlPositions(moveablePosition);

                // 스킬을 사용할 수 있는 위치에 타겟위치가 포함된다면 리스트에 추가한다.
                if (SkillablePositions.Contains(targetPosition))
                    TargetedMovablePositions.Add(moveablePosition);
            }
            return TargetedMovablePositions;
        }

        /// <summary>
        /// 위치들을 받아서 가장 우선순위에 적합한 한 위치를 반환하는 함수 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="positions"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        private static Vector2Int? GetPriorityPosition(Unit user, List<Vector2Int> positions, Priority priority)
        {
            Vector2Int targetPosition;

            if (positions.Count > 0)
                targetPosition = positions[0];
            else
                return null; // 아무데도 사용할수 없다면 null를 리턴합니다.

            for (int i = 1; i < positions.Count; i++)
            {
                Debug.Log(user.Name + ": " + priority + " , " + positions[i]);
                // 타겟 우선순위가 나로부터 가장 가까운 위치이다.
                if (priority == Priority.NearFromMe &&
                    (targetPosition - user.Position).magnitude > (positions[i] - user.Position).magnitude)
                    targetPosition = positions[i];
                // 타겟 우선순위가 나로부터 가장 먼 위치이다.
                else if (priority == Priority.FarFromMe &&
                    (targetPosition - user.Position).magnitude < (positions[i] - user.Position).magnitude)
                    targetPosition = positions[i];
                // 타겟 우선순위가 현재 HP가 가장 많은 유닛의 위치이다.
                else if (priority == Priority.BiggerCurHP &&
                    BattleManager.GetUnit(targetPosition).CurHP < BattleManager.GetUnit(positions[i]).CurHP)
                    targetPosition = positions[i];
                // 타겟 우선순위가 현재 HP가 가장 적은 유닛의 위치이다.
                else if (priority == Priority.SmallerCurHP &&
                    BattleManager.GetUnit(targetPosition).CurHP > BattleManager.GetUnit(positions[i]).CurHP)
                    targetPosition = positions[i];
                // 타겟 우선순위가 파티들의 평균위치로부터 멀거나 가까운 위치이다.
                else if (priority == Priority.NearFromPartys || priority == Priority.FarFromPartys)
                {
                    List<Unit> partyUnits = BattleManager.GetUnit(UnitAlliance.Party);
                    Vector2Int averagePosition = new Vector2Int();
                    foreach (var unit in partyUnits)
                        averagePosition += unit.Position;
                    averagePosition /= partyUnits.Count == 0 ? 1 : partyUnits.Count;

                    if (priority == Priority.NearFromPartys &&
                        (targetPosition - averagePosition).magnitude > (positions[i] - averagePosition).magnitude)
                        targetPosition = positions[i];
                    else if (priority == Priority.FarFromPartys &&
                        (targetPosition - averagePosition).magnitude < (positions[i] - averagePosition).magnitude)
                        targetPosition = positions[i];
                }
                // 타겟 우선순위가 가장가까운 파티원으로부터 멀거나 가까운 위치이다.
                else if (priority == Priority.NearFromClosestParty || priority == Priority.FarFromClosestParty)
                {
                    List<Unit> partyUnits = BattleManager.GetUnit(UnitAlliance.Party);
                    Unit closestUnit = partyUnits[0];
                    float distance = (user.Position - partyUnits[0].Position).magnitude;
                    foreach (var unit in partyUnits)
                        if (distance > (user.Position - unit.Position).magnitude)
                            closestUnit = unit;

                    if (priority == Priority.NearFromClosestParty &&
                        (targetPosition - closestUnit.Position).magnitude > (positions[i] - closestUnit.Position).magnitude)
                        targetPosition = positions[i];
                    else if (priority == Priority.FarFromClosestParty &&
                        (targetPosition - closestUnit.Position).magnitude < (positions[i] - closestUnit.Position).magnitude)
                        targetPosition = positions[i];
                }
            }

            return targetPosition;
        }

        /// <summary>
        /// 어디에 스킬을 사용할지 위치를 반환하는 함수
        /// </summary>
        /// <param name="user"></param>
        /// <param name="skill"></param>
        /// <returns></returns>
        private static Vector2Int? GetTargetPosition(Unit user, Skill skill)
        {
            // 사용할수 있는 위치들
            List<Vector2Int> TargetPositions = GetMovedSkillablePositions(user, skill);

            // 사용할수 있는 위치들에서 우선순위에 가장 적합한 스킬 사용 위치를 하나 받아온다.
            return GetPriorityPosition(user, TargetPositions, skill.Priority);
        }

        /// <summary>
        /// 정해진 타겟 위치를 기반으로 어디로 이동할지의 위치를 반환하는 함수
        /// </summary>
        /// <param name="user"></param>
        /// <param name="skill"></param>
        /// <param name="targetPosition"></param>
        /// <returns></returns>
        private static Vector2Int? GetMovePosition(Unit user, Skill skill, Vector2Int targetPosition)
        {
            if (user.MoveSkill.IsUsable() == true)
            {
                List<Vector2Int> MovePositions = GetTargetedMoveablePositions(user, skill, targetPosition);
                return GetPriorityPosition(user, MovePositions, user.MoveSkill.Priority);
            }
            else
                return null;
        }

        /// <summary>
        /// 사용가능한 스킬이 없을 경우에 스킬을 고려하지 않고 이동할 위치를 구한다.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private static Vector2Int? GetMovePosition(Unit user)
        {
            if (user.MoveSkill.IsUsable() == true)
            {
                // 자기 자신의 위치 추가
                List<Vector2Int> MovePositions = user.MoveSkill.GetAvlUsePositions();
                MovePositions.Add(user.Position);

                Priority priority = user.MoveSkill.Priority;

                // 내가 원하는 대상에까지 가는 경로
                List<Vector2Int> path;

                if (priority == Priority.NearFromPartys)
                {
                    List<Unit> partyUnits = BattleManager.GetUnit(UnitAlliance.Party);
                    Vector2Int averagePosition = new Vector2Int();
                    foreach (var unit in partyUnits)
                        averagePosition += unit.Position;
                    averagePosition /= partyUnits.Count == 0 ? 1 : partyUnits.Count;

                    Vector2Int? dest = PathFind.GetClosestReachableDest(user, averagePosition)?.position;
                    if (dest == null) return null;

                    path = PathFind.PathFindAlgorithm(user.MoveSkill, user.Position, (Vector2Int)dest);

                    if (path != null && path.Count > 1)
                        return path[1];
                    else
                        return null;
                }
                // 타겟 우선순위가 가장가까운 파티원으로부터 멀거나 가까운 위치이다.
                else if (priority == Priority.NearFromClosestParty)
                {
                    List<Unit> partyUnits = BattleManager.GetUnit(UnitAlliance.Party);
                    Unit closestUnit = partyUnits[0];
                    float distance = (user.Position - partyUnits[0].Position).magnitude;
                    foreach (var unit in partyUnits)
                        if (distance > (user.Position - unit.Position).magnitude)
                            closestUnit = unit;

                    Vector2Int? dest = PathFind.GetClosestReachableDest(user, closestUnit.Position)?.position;
                    if (dest == null) return null;

                    path = PathFind.PathFindAlgorithm(user.MoveSkill, user.Position, (Vector2Int)dest);

                    if (path != null)
                        return path[1];
                    else
                        return null;
                }
                else
                    return GetPriorityPosition(user, MovePositions, user.MoveSkill.Priority);
            }
            else
                return null;
        }
    }
}