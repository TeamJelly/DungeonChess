﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleUI : MonoBehaviour
{
    public static BattleUI instance;

    public GameObject unitsInfoPanel;
   
    public GameObject BlueTile; // 선택 가능한 타일에 사용한다.
    public GameObject YelloTile; // 이동할 위치, 스킬 기준점 타일에 사용
    public GameObject OrangeTile; // 스킬 추가 위치 타일에 사용한다.

    public GameObject UnitInfoUIPrefab;

    [Header("Buttons")]
    public Button currentPushedButton; // 현재 누르고 있는 버튼
    public Button moveButton;
    public List<Button> skillButtons;
    public List<Button> itemButtons;

    List<UnitInfoUI> UnitsInfoList = new List<UnitInfoUI>();
    public UnitInfoUI selectedUnitInfo;
    public EventTrigger[,] AllTiles = new EventTrigger[10,10];

    Transform tileIndicator;
    UnitPosition indicatorUnitPosition = new UnitPosition();

    RectTransform unitTurnIndicator;

    private void Awake()
    {
        instance = this;
    }

    public void Init()
    {
        //전체 유닛들 클릭시 유닛 설명창 활성화 기능 추가
        foreach(Unit unit in BattleManager.instance.AllUnits)
        {
            unit.gameObject.AddComponent<BoxCollider2D>();
            EventTrigger eventTrigger = unit.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry_PointerClick = new EventTrigger.Entry();
            entry_PointerClick.eventID = EventTriggerType.PointerClick;
            entry_PointerClick.callback.AddListener((data) =>
            {
                UnitDescriptionUI.instance.Enable(unit);
            });
            eventTrigger.triggers.Add(entry_PointerClick);
        }

        //파티원 유닛과 대응하는 UI 생성.
        for(int i = 0; i < PartyManager.instance.AllUnits.Count; i++)
        {
            GameObject g = Instantiate(UnitInfoUIPrefab);
          
            g.transform.SetParent(unitsInfoPanel.transform);
            g.transform.SetAsFirstSibling();
            g.transform.localScale = Vector3.one;

            UnitsInfoList.Add(g.GetComponent<UnitInfoUI>());

        }

        //해당 ui 초기화
        foreach (UnitInfoUI unitInfoUI in UnitsInfoList) unitInfoUI.Init();

        //이동시 선택하는 타일 전부 생성.
        Transform allTilesParent = new GameObject("TileSelector").transform;
        for(int i = 0; i < 10; i++)
            for(int j = 0; j < 10; j++)
            {
                AllTiles[i, j] = Instantiate(BlueTile).GetComponent<EventTrigger>();
                AllTiles[i, j].transform.SetParent(allTilesParent);
                AllTiles[i, j].transform.position = new Vector3(i, j,-2);
                AllTiles[i, j].gameObject.SetActive(false);
                
            }

        //타일 선택기 생성
        tileIndicator = Instantiate(YelloTile).transform;
        tileIndicator.gameObject.SetActive(false);

        //유닛 턴 표시기 생성
        unitTurnIndicator = new GameObject().AddComponent<RectTransform>();
        unitTurnIndicator.SetParent(this.transform);
        unitTurnIndicator.sizeDelta = new Vector2(245, 120);
        unitTurnIndicator.gameObject.AddComponent<Image>().color = Color.yellow;
        unitTurnIndicator.localScale = Vector3.one;
        unitTurnIndicator.gameObject.SetActive(false);
        
    }
      

    //턴종료 버튼 이벤트
    public void SetNextTurn()
    {
        BattleManager.instance.SetNextTurn();
    }

    IEnumerator ShowMoveAnimation()
    {
        Unit unit = BattleManager.instance.thisTurnUnit;
        List<UnitPosition> path = BattleManager.PathFindAlgorithm(unit, unit.unitPosition, indicatorUnitPosition);

        var waitForSeconds = new WaitForSeconds(0.1f);
        foreach (UnitPosition position in path)
        {
            unit.Move(position);
            yield return waitForSeconds;
        }
    }

    //유닛 정보창 갱신.
    public void UpdateInfoList()
    {
        for (int i = 0; i < PartyManager.instance.AllUnits.Count; i++)
        {
            Unit unit = PartyManager.instance.AllUnits[i];
            UnitsInfoList[i].Set(unit);

            if(unit == BattleManager.instance.thisTurnUnit)
            {
                unitTurnIndicator.SetParent(UnitsInfoList[i].transform);
                unitTurnIndicator.SetAsFirstSibling();
                unitTurnIndicator.anchoredPosition = Vector2.zero;
            }
        }
    }
    public void InitThisTurnPanel(Unit unit)
    {
        currentPushedButton = null;
        HideTile();

        UpdateInfoList();

        if (PartyManager.instance.AllUnits.Contains(unit)) // 파티원인가?
        {
            // Debug.LogError("init this turn panel" + unit.name);

            foreach (var item in skillButtons)
            {
                item.interactable = false;
                item.GetComponent<Image>().sprite = null;
            }

            for (int i = 0; i < unit.skills.Count; i++)
            {
                skillButtons[i].GetComponent<Image>().sprite = unit.skills[i].skillImage;
            }

            UpdateThisTurnPanel();

            selectedUnitInfo.gameObject.SetActive(true);
            unitTurnIndicator.gameObject.SetActive(true);
        }
        else
        {
            selectedUnitInfo.gameObject.SetActive(false);
            unitTurnIndicator.gameObject.SetActive(false);
        }
    }

    public void UpdateThisTurnPanel()
    {
        UpdateThisTurnPanel(BattleManager.instance.thisTurnUnit);
    }

    public void UpdateThisTurnPanel(Unit unit)
    {
        selectedUnitInfo.Set(unit);

        moveButton.interactable = false; // 이동 버튼 비활성화
        foreach (Button skillButton in skillButtons) // 스킬 버튼 비활성화
            skillButton.interactable = false;
        foreach (Button itemButton in itemButtons) // 스킬 버튼 비활성화
            itemButton.interactable = false;

        if (currentPushedButton == null) // 아무 버튼도 안눌린 기본 상태
        {
            if (unit.moveCount > 0) // 이동 가능한가?
            {
                moveButton.interactable = true;
                moveButton.onClick.RemoveAllListeners();

                moveButton.onClick.AddListener(() => // 이동 버튼을 눌렀을 때 작동하는 코드
                {
                    currentPushedButton = moveButton;

                    ShowMovableTile();

                    UpdateThisTurnPanel(); // 다시 UI 업데이트 호출
                });
            }

            for (int i = 0; i < unit.skills.Count; i++)
            {
                Skill skill = unit.skills[i];

                if (skill.currentReuseTime == 0 && unit.skillCount > 0) // 스킬이 사용가능한 조건
                {
                    skillButtons[i].interactable = true;
                    skillButtons[i].onClick.RemoveAllListeners();

                    int temp = i;

                    skillButtons[i].onClick.AddListener(() => // 스킬 버튼을 눌렀을 때 작동하는 코드
                    {
                        currentPushedButton = skillButtons[temp];

                        ShowSkillableTile(skill.GetPositionsInDomain(unit), skill); // 파란, 노란 타일 추가
                        SetSkillIndicator(skill); // 스킬 사용시 추가 범위 표시기 (오렌지 타일) 추가 

                        UpdateThisTurnPanel();
                    });

                }
            }

            for (int i = 0; i < unit.items.Count; i++)
            {
                //아이템 버튼
            }
        }
        else // 버튼 눌린 상태, 타 버튼 비활성화, 다시 누르면 타일 표시기들 제거
        {
            currentPushedButton.interactable = true; // 취소버튼만 활성화

            currentPushedButton.onClick.RemoveAllListeners();
            currentPushedButton.onClick.AddListener(() =>
            {
                HideTile();

                currentPushedButton = null;
                UpdateThisTurnPanel();

            });
        }
    }

    public void SetSkillIndicator(Skill skill) // 스킬용 인디케이터 보여주기
    {
        if (skill.target == Skill.Target.AnyTile || skill.target == Skill.Target.NoUnitTile)
        {
            //스킬 종류마다 다름 유닛용 인디케이터, 타일용 인티케이터 구분 필요.
            tileIndicator.localScale = new Vector3(1, 1, 1);

            foreach (var item in skill.RangePositions)
            {
                Instantiate(OrangeTile, tileIndicator).transform.localPosition = new Vector3(item.x, item.y, 0);
            }

        } else if (skill.target == Skill.Target.AnyUnit || skill.target == Skill.Target.EnemyUnit 
            || skill.target == Skill.Target.FriendlyUnit || skill.target == Skill.Target.PartyUnit)
        {
            // 유닛 선택용 인디케이터 필요
        }
    }

    public void SetIndicatorUnitPosition(Vector2Int position)
    {
        SetIndicatorUnitPosition(new UnitPosition(position, position));
    }

    public void SetIndicatorUnitPosition(UnitPosition position)
    {
        indicatorUnitPosition.Set(position);//깊은복사

        tileIndicator.localScale =
                new Vector3(
                    position.upperRight.x - position.lowerLeft.x + 1,
                    position.upperRight.y - position.lowerLeft.y + 1,
                    1
                );

        Vector3 screenPosition = position.lowerLeft + (Vector2)(position.upperRight - position.lowerLeft) / 2;
        screenPosition.z = -4;
        tileIndicator.localPosition = screenPosition;
        tileIndicator.gameObject.SetActive(true);
    }

    public void ShowSkillableTile(List<Vector2Int> positions, Skill skill)
    {
        foreach (Vector2Int position in positions)
        {
            SetSkillTile(position.x, position.y, skill);
            AllTiles[position.x, position.y].gameObject.SetActive(true);
        }
        SetIndicatorUnitPosition(BattleManager.instance.thisTurnUnit.unitPosition.upperRight);
    }

    public void SetSkillTile(int x, int y, Skill skill)
    {
        AllTiles[x, y].triggers.Clear();
        //타일 클릭시 이벤트 추가
        EventTrigger.Entry entry_PointerClick = new EventTrigger.Entry();
        entry_PointerClick.eventID = EventTriggerType.PointerClick;
        entry_PointerClick.callback.AddListener((data) =>
        {
            HideTile();

            if (skill.target == Skill.Target.AnyTile || skill.target == Skill.Target.NoUnitTile)
            {
                List<Vector2Int> temp = new List<Vector2Int>();
                foreach (var item in skill.RangePositions)
                    temp.Add(new Vector2Int(x, y) + item);

                skill.UseSkillToTile(temp);
            }
            /*else if (skill.target == Skill.Target.AnyUnit || skill.target == Skill.Target.EnemyUnit
              || skill.target == Skill.Target.FriendlyUnit || skill.target == Skill.Target.PartyUnit)
            {
                List<Unit> temp = new List<Unit>();
                foreach (var item in skill.RangePositions)
                {
                    Vector2Int vector = new Vector2Int(x, y) + item;
                    temp.Add(BattleManager.instance.AllTiles[vector.x, vector.y].GetUnit());
                }

                skill.UseSkillToUnit(temp);
            }*/

            // UI 업데이트
            BattleManager.instance.thisTurnUnit.moveCount--;
            BattleManager.instance.thisTurnUnit.skillCount--;
            currentPushedButton = null;
            UpdateThisTurnPanel();
        });
        AllTiles[x, y].triggers.Add(entry_PointerClick);

        //타일 위로 마우스 지나갈 때 이벤트 추가 
        EventTrigger.Entry entry_PointerEnter = new EventTrigger.Entry();
        entry_PointerEnter.eventID = EventTriggerType.PointerEnter;
        entry_PointerEnter.callback.AddListener((data) =>
        {
            Vector2Int distance = new Vector2Int();
            if (indicatorUnitPosition.lowerLeft.x > x)
            {
                distance.x = x - indicatorUnitPosition.lowerLeft.x;
            }
            else if (indicatorUnitPosition.upperRight.x < x)
            {
                distance.x = x - indicatorUnitPosition.upperRight.x;
            }
            if (indicatorUnitPosition.lowerLeft.y > y)
            {
                distance.y = y - indicatorUnitPosition.lowerLeft.y;
            }
            else if (indicatorUnitPosition.upperRight.y < y)
            {
                distance.y = y - indicatorUnitPosition.upperRight.y;
            }

            for (int k = indicatorUnitPosition.lowerLeft.x; k <= indicatorUnitPosition.upperRight.x; k++)
            {
                for (int l = indicatorUnitPosition.lowerLeft.y; l <= indicatorUnitPosition.upperRight.y; l++)
                {
                    if (!AllTiles[k + distance.x, l + distance.y].gameObject.activeSelf) return;
                }
            }

            indicatorUnitPosition.Add(distance);

            SetIndicatorUnitPosition(indicatorUnitPosition);
        });
        AllTiles[x, y].triggers.Add(entry_PointerEnter);
    }

    public void ShowMovableTile()
    {
        HideTile();
        SetIndicatorUnitPosition(BattleManager.instance.thisTurnUnit.unitPosition);
        List<UnitPosition> positions = BattleManager.instance.thisTurnUnit.GetMovablePosition();
      
        foreach (UnitPosition unitPosition in positions)
            for (int i = unitPosition.lowerLeft.x; i <= unitPosition.upperRight.x; i++)
            {
                for (int j = unitPosition.lowerLeft.y; j <= unitPosition.upperRight.y; j++)
                {
                    SetMoveTile(i,j);
                    AllTiles[i, j].gameObject.SetActive(true);
                }
            }
    }

    void SetMoveTile(int x, int y)
    {
        AllTiles[x, y].triggers.Clear();
        //타일 클릭시 이벤트 추가
        EventTrigger.Entry entry_PointerClick = new EventTrigger.Entry();
        entry_PointerClick.eventID = EventTriggerType.PointerClick;
        entry_PointerClick.callback.AddListener((data) =>
        {
            HideTile();
            StartCoroutine(ShowMoveAnimation());

            BattleManager.instance.thisTurnUnit.moveCount--;
            currentPushedButton = null;
            UpdateThisTurnPanel();
        });
        AllTiles[x, y].triggers.Add(entry_PointerClick);

        //타일 위로 마우스 지나갈 때 이벤트 추가 
        EventTrigger.Entry entry_PointerEnter = new EventTrigger.Entry();
        entry_PointerEnter.eventID = EventTriggerType.PointerEnter;
        entry_PointerEnter.callback.AddListener((data) =>
        {
            Vector2Int distance = new Vector2Int();
            if (indicatorUnitPosition.lowerLeft.x > x)
            {
                distance.x = x - indicatorUnitPosition.lowerLeft.x;
            }
            else if (indicatorUnitPosition.upperRight.x < x)
            {
                distance.x = x - indicatorUnitPosition.upperRight.x;
            }
            if (indicatorUnitPosition.lowerLeft.y > y)
            {
                distance.y = y - indicatorUnitPosition.lowerLeft.y;
            }
            else if (indicatorUnitPosition.upperRight.y < y)
            {
                distance.y = y - indicatorUnitPosition.upperRight.y;
            }

            for (int k = indicatorUnitPosition.lowerLeft.x; k <= indicatorUnitPosition.upperRight.x; k++)
            {
                for (int l = indicatorUnitPosition.lowerLeft.y; l <= indicatorUnitPosition.upperRight.y; l++)
                {
                    if (!AllTiles[k + distance.x, l + distance.y].gameObject.activeSelf) return;
                }
            }
            indicatorUnitPosition.Add(distance);

            SetIndicatorUnitPosition(indicatorUnitPosition);
        });
        AllTiles[x, y].triggers.Add(entry_PointerEnter);
    }

    public void HideTile()
    {
        tileIndicator.gameObject.SetActive(false);

        for (int i = 0; i < tileIndicator.childCount; i++)
            Destroy(tileIndicator.GetChild(i).gameObject);

        foreach (var tile in AllTiles)
            if (tile.gameObject.activeSelf)
                tile.gameObject.SetActive(false);

    }



}
