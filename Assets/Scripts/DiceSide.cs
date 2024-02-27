using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DiceSide : MonoBehaviour
{
    private int _sideValue;
    public int SideValue  //in case of dynamic change of dice side values
    {
        get
        {
            return _sideValue;
        }
        set 
        { 
            _sideValue = value;

            if (text == null)
                text = GetComponentInChildren<TextMeshPro>(); //for dice sides editor purpose

            text.text = value.ToString();
        }
    }
    private TextMeshPro text;

    private void Awake()
    {
        text = GetComponentInChildren<TextMeshPro>();
    }
}
