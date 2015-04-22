using UnityEngine;
using System.Collections;

public class LGameData : MonoBehaviour {



    public const string PlayerPrefs_LevelTotal = "GameLevelTotal";
    public const string PlayerPrefs_LevelUnlocked = "GameLevelUnlocked";
    public const string PlayerPrefs_LevelCurrent = "GameLevelCurrent";
    private const string PlayerPrefs_LevelStatus = "GameStatus";

    public const int IdGameMenu = 0;
    public const int IdGameLevel = 1;
    public const int IdGameSetting = 2;
    public const int IdGameHelp = 3;
    public const int IdGameOver = 4;
    public const int IdGameReturn = 5;

    public const int IdBtnLevel = 6;
    public const int IdBtnSetting = 7;
    public const int IdBtnHelp = 8;
    public const int IdBtnReturn = 9;
    public const int IdBtnExit = 10;
    public const int IdBtnReplay = 11;

    public const int IdBtnAboutUs = 12;
    public const int IdBtnAboutMonster = 13;
    public const int IdBtnAboutTower = 14;


    public const int IdLevelSize=4;//游戏等级数
    public const int IdLevel1 = 0;
    public const int IdLevel2 = 1;
    public const int IdLevel3 = 2;
    public const int IdLevel4 = 3;

    public const string StrLevel1 = "ARgame_1";
    public const string StrLevel2 = "ARgame_2";
    public const string StrLevel3 = "ARgame_3";
    public const string StrLevel4 = "ARgame_4";

    public const float TimeSlow = 0;
    public const float TimeFast = 0;

    void Start()
    {
    }
    void Update()
    {
    }

    void OnDesDroy()
    {
    }

}
