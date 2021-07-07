﻿using Model;
using Model.Managers;
using System.Collections;
using System.Collections.Generic;
using UI.Battle;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace View
{
    public class BattleView : MonoBehaviour
    {
        private GameObject hPBarPrefab;

        public Image currentUnitPortrait;

        // public UnitInfoView ThisTurnUnitInfo;
        // public UnitInfoView OtherUnitInfo { get; set; }

        public Button TurnEndButton { get; private set; }
        public Dictionary<Unit, GameObject> UnitObjects { get; } = new Dictionary<Unit, GameObject>();
        public Dictionary<Unit, HPBarView> HPBars { get; } = new Dictionary<Unit, HPBarView>();

        public UnitControlUI unitControlUI;
        private void Awake()
        {
            hPBarPrefab = Resources.Load<GameObject>("Prefabs/UI/Battle/HP_BAR");

            //ThisTurnUnitInfo = transform.Find("Panel/ThisTurnUnitInfo").GetComponent<UnitInfoView>();
            //OtherUnitInfo = transform.Find("Panel/OtherUnitInfo").GetComponent<UnitInfoView>();
            //OtherUnitInfo.gameObject.SetActive(false);

            TurnEndButton = transform.Find("MainPanel/TurnEndButton").GetComponent<Button>();
            unitControlUI = GetComponent<UnitControlUI>();

            if (!GameManager.InBattle)
            {
                TurnEndButton.gameObject.SetActive(false);
                unitControlUI.panel.SetActive(false);
            }
        }
        public IEnumerator MoveLeaderUnit()
        {
            IEnumerator coroutine = GameManager.LeaderUnit.MoveSkill.Use(GameManager.LeaderUnit, Vector2Int.zero);
            while (true)
            {
                if(Input.GetKeyDown(KeyCode.Mouse1))
                {
                    Vector3 mousepos = Camera.main.ScreenToWorldPoint(Input.mousePosition) + Vector3.one * 0.5f;
                    Vector2Int destination = new Vector2Int((int)mousepos.x, (int)mousepos.y);

                    // 리더 유닛이 해당 타일에 위치가능하다면
                    if (FieldManager.GetTile(destination).IsUnitPositionable(GameManager.LeaderUnit))
                    {
                        //리더 유닛 이동 코루틴 실행. 기존 실행되던 코루틴은 정지.
                        StopCoroutine(coroutine);
                        coroutine = GameManager.LeaderUnit.MoveSkill.Use(GameManager.LeaderUnit, destination);
                        StartCoroutine(coroutine);
                    }
                }
                yield return null;
            }
        }

        public void SummonPartyUnits(int index = 0)
        {
            Unit unit = GameManager.PartyUnits[index];

            currentUnitPortrait.sprite = unit.Portrait;

            IndicatorUI.ShowTileIndicator(FieldManager.instance.GetStairAroundPosition(),
                (Vector2Int position) =>
                {
                    Common.UnitAction.Summon(unit, position);
                    index++;

                    if (index == GameManager.PartyUnits.Count)
                    {
                        IndicatorUI.HideTileIndicator();
                        BattleController.instance.NextTurnStart();
                    }
                    // 전부 소환할때까지 재귀로 돈다.
                    else
                        SummonPartyUnits(index);
                }
            );
        }

        private void Update()
        {
            foreach(Unit unit in HPBars.Keys)
            {
                Vector3 unitPos = new Vector3(unit.Position.x, unit.Position.y);
                HPBars[unit].SetPosition(unitPos);
            }
            if (Input.GetKeyDown(KeyCode.Space))
                TurnEndButton.onClick.Invoke();
        }

        public void SetTurnEndButton(UnityAction action)
        {
            TurnEndButton.onClick.RemoveAllListeners();
            TurnEndButton.onClick.AddListener(action);
        }

        public void SetTurnUnitPanel(Unit unit)
        {
            currentUnitPortrait.sprite = unit.Portrait;

            TurnEndButton.gameObject.SetActive(true);
            unitControlUI.panel.SetActive(true);

            unitControlUI?.UpdateUI(unit);

            //if (unit.Category != Category.Party)
            //    ThisTurnUnitInfo?.SetUnitInfo(unit, false);
            //else
            //    ThisTurnUnitInfo?.SetUnitInfo(unit, true);
        }

        /// <summary>
        /// 유닛을 필드에 오브젝트로 생성시키는 함수
        /// 생성하면서 이벤트 콜벡을 할당해줘야 한다.
        /// </summary>
        /// <param name="unit"></param>
        public void MakeUnitObject(Unit unit)
        {
            // 미리 존재 여부 확인
            if (UnitObjects.ContainsKey(unit) == true)
            {
                Debug.LogError($"이미 필드에 유닛({unit.Name}) 오브젝트가 존재합니다.");
                return;
            }

            // 게임 오브젝트 생성
            GameObject newObj = new GameObject(unit.Name);

            // 위치 지정
            newObj.transform.position = new Vector3(unit.Position.x, unit.Position.y, 0);

            // 박스 콜라이더 컴포넌트 추가
            BoxCollider2D boxCollider2D = newObj.AddComponent<BoxCollider2D>();
            boxCollider2D.size = new Vector2(1, 1);

            // 이벤트 트리거 컴포넌트 추가
            EventTrigger eventTrigger = newObj.AddComponent<EventTrigger>();

            // 스프라이터 랜더러 추가
            SpriteRenderer spriteRenderer = newObj.AddComponent<SpriteRenderer>();

            // 애니메이터 추가
            Animator animator = newObj.AddComponent<Animator>();
            animator.runtimeAnimatorController = unit.Animator;

            // 이벤트 트리거 설정
            EventTrigger.Entry entry_PointerClick = new EventTrigger.Entry();
            entry_PointerClick.eventID = EventTriggerType.PointerClick;
            entry_PointerClick.callback.AddListener((data) =>
            {
//                OtherUnitInfo.gameObject.SetActive(true);
//                OtherUnitInfo.SetUnitInfo(unit, false);
            });
            eventTrigger.triggers.Add(entry_PointerClick);
            UnitObjects.Add(unit, newObj);

            // HP 바 생성
            HPBarView newHPBar = Instantiate(hPBarPrefab, ViewManager.instance.MainPanel).GetComponent<HPBarView>();
            newHPBar.Init(unit);
            HPBars.Add(unit, newHPBar);

            //유닛 오브젝트 상호작용 콜백 등록
            unit.OnPosition.changed.AddListener((v) =>
            {
                Vector3 w = new Vector3(v.x, v.y);
                newObj.transform.position = w;
                newHPBar.SetPosition(w);
            });
            unit.OnCurHP.changed.AddListener(newHPBar.SetValue);

            //최초 갱신
            //newHPBar.SetValue(unit.CurHP);
            int tempHP = unit.CurHP;
            Vector2Int tempPosition = unit.Position;

            unit.OnPosition.changed.Invoke(ref tempPosition);
            unit.OnCurHP.changed.Invoke(ref tempHP);
        }

        public void DestroyUnitObject(Unit unit)
        {
            GameObject unitObj = UnitObjects[unit];
            UnitObjects.Remove(unit);
            Destroy(unitObj);

            HPBarView hpBar = HPBars[unit];
            HPBars.Remove(unit);
            Destroy(hpBar.gameObject);

            unit.OnPosition.changed.RemoveAllListeners();
            unit.OnCurHP.changed.RemoveAllListeners();

            // AgilityViewer.instance.DestroyObject(unit);

        }
    }
}