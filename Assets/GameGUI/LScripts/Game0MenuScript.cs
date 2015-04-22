using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Game0MenuScript : LGameData
{
    public Button btnLevel;
    public Button btnSetting;
    public Button btnHelp;
    public Button btnExit;
    public LGameControl control;

    // Use this for initialization
    void Start()
    {
        btnLevel.onClick.AddListener(delegate() { control.LGameClick(IdBtnLevel); });
        btnSetting.onClick.AddListener(delegate() { control.LGameClick(IdBtnSetting); });
        btnHelp.onClick.AddListener(delegate() { control.LGameClick(IdBtnHelp); });
        btnExit.onClick.AddListener(delegate() { control.LGameClick(IdBtnExit); });
    }

}
