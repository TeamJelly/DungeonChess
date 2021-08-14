﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace View.UI
{
    public class SimpleInfo : MonoBehaviour
    {
        public TextMeshProUGUI Name;
        public Image Image;
        public TextMeshProUGUI Description;

        public void Set(Infoable infoable)
        {
            Set(infoable.Name, infoable.Sprite, infoable.Color, infoable.Description);
        }
        public void Set(string name, Sprite sprite, Color color, string description)
        {
            Name.text = name;
            
            Image.sprite = sprite;
            if (color != new Color(0,0,0,0))
                Image.color = color;
            else
                Image.color = Color.white;

            Description.text = description;
        }
        private void OnValidate()
        {
            Name = transform.Find("Name/Text").GetComponent<TextMeshProUGUI>();
            Image = transform.Find("ImageMask/Image").GetComponent<Image>();
            Description = transform.Find("Description/Text").GetComponent<TextMeshProUGUI>();
        }

    }
}
