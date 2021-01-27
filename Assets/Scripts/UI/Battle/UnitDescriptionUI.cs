﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Common.UI;
using Model.Models;

namespace UI.Battle
{
    //유닛 누르면 팝업 형식으로 뜨는 ui
    public class UnitDescriptionUI : MonoBehaviour
    {
        public Text text;
        public static UnitDescriptionUI instance;
        //여기에 유물정보 있으면 좋을 것 같음.

        private void Awake()
        {
            instance = this;
            gameObject.SetActive(false);
        }

        public void Enable(Unit unit)
        {
            text.TextString =
                "이름: " + unit.name +
                "\nstrength: " + unit.strength;
            gameObject.SetActive(true);
        }

        public void Disable()
        {
            gameObject.SetActive(false);
        }
    }
}