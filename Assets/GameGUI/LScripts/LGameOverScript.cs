using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LGameOverScript : MonoBehaviour {

    public Button LBtnNext;
    public Button LBtnReplay;
    public Text LOverMsg;


    private const string PlayerPrefs_LevelTotal = "GameLevelTotal";
    private const string PlayerPrefs_LevelUnlocked = "GameLevelUnlocked";
    private const string PlayerPrefs_LevelCurrent = "GameLevelCurrent";
    private const string PlayerPrefs_LevelStatus = "GameStatus";

    public string[] GameLevelsGroup;
//    public Button Replay;
//    public Button NextScene;

    //下一个要进入的场景名称
    private string GameOver_NextScene;

    private const int GameOverClick_ForBackMenu = 0;
    private const int GameOverClick_ForNextLevel = 1;
    private const int GameOverClick_ForReplay = 2;

    private const string GameOverClick_NextSceneMenu="LGameMenu";

    private int GameLevelUnlocked;
    private int GameLevelCurrent;
    private int GameLevelTotal;
    private string GameStatus;

    public void LGameOverClick(int i)
    {
//        print("GameOverClick "+ i );


        switch (i)
        {
            case GameOverClick_ForBackMenu:
                {
                    //GameInvokeNewScene(GameOverClick_NextSceneMenu);
                    Application.LoadLevel(0);
                    break;
                }
            case GameOverClick_ForReplay: {
                //要重玩本关卡?
			GameInvokeNewScene(GameLevelsGroup[GameLevelCurrent]);
                break; }
            case GameOverClick_ForNextLevel:{
                GameLevelCurrent++;
                GameInvokeNewScene(GameLevelsGroup[GameLevelCurrent]);
                print("下一关" + GameLevelCurrent);
                PlayerPrefs.SetInt(PlayerPrefs_LevelCurrent,GameLevelCurrent);
                break;
            }
        }
    }

    void show(int which)
    {
        switch (which)
        {
            case 1:{
                //赢了，不到最后一关
                LBtnNext.active = true;
                LBtnReplay.active = false;

                LOverMsg.text = "恭喜你，通过本关，要不要玩下一关呢？";
                break;
            }
            case 2: {
                //赢了，是最后一关
                LBtnNext.active = false;
                LBtnReplay.active = true;
                LOverMsg.text = "恭喜你，通过全部关卡，真是太厉害啦！";
                break;
            }
            case 3: {
                //输了
                LBtnNext.active = false;
                LBtnReplay.active = true;
                LOverMsg.text = "哎呦，这关输了呀，不服吗？再来一次？";
                break;
            }
        }
    }

	// Use this for initialization
	void Start () {

//        print("start");
        GameLevelUnlocked = PlayerPrefs.GetInt(PlayerPrefs_LevelUnlocked);
        GameLevelCurrent = PlayerPrefs.GetInt(PlayerPrefs_LevelCurrent);
        GameLevelTotal = PlayerPrefs.GetInt(PlayerPrefs_LevelTotal);
        GameStatus = PlayerPrefs.GetString(PlayerPrefs_LevelStatus);

        if (GameStatus == "WIN")
        {
            if (GameLevelCurrent == GameLevelTotal-1)//到最后一关
            {
                show(2);
            }
            else
            {
                show(1);
            }
        }
        else
        {
            show(3);
        }

	}
	
	// Update is called once per frame
	void Update () {
	
	}
    private void GameInvokeNewScene(string scene)
    {
        this.GameOver_NextScene = scene;

        if (!IsInvoking("LoadGameScene"))
        {
            InvokeRepeating("LoadGameScene", 0f, 0.2f);
        }
    }


    void LoadGameScene()
    {
//        print("LoadGameScene   ......");
        print("Load    = " + GameOver_NextScene);
        if (Application.CanStreamedLevelBeLoaded (GameOver_NextScene)) {
//            print("loading....");
						Application.LoadLevel (GameOver_NextScene);
				} else
						;
//            print("preparing....");
    }
}
