using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LGameMenuScript : MonoBehaviour
{
    public GameObject GameMenuGroup;
    public GameObject GameLevelGroup;
    public GameObject GameSettingGroup;
    public GameObject GameHelpGroup;
    public GameObject GameOverGroup;

    

    private const int GameMenuClick_ForLevel = 0;
    private const int GameMenuClick_ForSetting = 1;
    private const int GameMenuClick_ForHelp = 2;
    private const int GameMenuClick_ForExit = 3;
    private const string GameMenuClick_NextSceneLevelName = "LGameLevel";
    private const string GameMenuClick_NextSceneHelpName = "LGameHelp";

    private const string loadGameMenu = "Game1Menu";
    private const string loadGameLevel = "Game2Level";
    private const string loadGameHelp = "Game4Help";
    private const string loadGameOver = "Game5Over";
    private const string loadGameSetting = "Game3Setting";

    private Vector3 GameSettingPosMiddle = new Vector3(Screen.width / 2, Screen.height - Screen.height / 8, 0);
    private Vector3 GameSettingPosTop = new Vector3(Screen.width / 2, Screen.height * 2, 0);
    private Vector3 GameSettingPosBottom = new Vector3(Screen.width / 2, -Screen.height * 2, 0);
    //下一个要进入的场景名称
    private string GameMenu_NextScene;
    //是否显示设置页
    private bool GameSettingState;

    //移动的屏内和屏外位置和时间
    private Vector3 GameSettingPosIn;
    private Vector3 GameSettingPosOut;
    private float GameSettingTime;


    void Start()
    {
        GameSettingPosIn = new Vector3(Screen.width / 2, Screen.height - Screen.height / 8, 0);
        GameSettingPosOut = new Vector3(Screen.width / 2, Screen.height * 2, 0);

        GameSettingState = false;
        GameSettingTime = 1f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

 

    public void MenuClick(int flag)
    {
        switch (flag)
        {
            case GameMenuClick_ForLevel:
                {
                    print("准备关卡选择页.....");

//                    this.GameMenu_NextScene = GameMenuClick_NextSceneLevelName;
                    GameInvokeNewScene(GameMenuClick_NextSceneLevelName);

                    break;
                }
            case GameMenuClick_ForSetting:
                {
                    print("准备设置页面.....");
                    if (GameSettingState == false)
                    {
                        GameSettingGroupIn(GameSettingGroup, GameSettingTime);
                        GameSettingState = true;
                    }
                    else
                    {
                        GameSettingGroupOut(GameSettingGroup, GameSettingTime);
                        GameSettingState = false;
                    }

                    break;
                }
            case GameMenuClick_ForHelp:
                {
                    print("准备游戏帮助页面.....");

//                    this.GameMenu_NextScene = GameMenuClick_NextSceneHelpName;
                    GameInvokeNewScene(GameMenuClick_NextSceneHelpName);

                    break;
                }
            case GameMenuClick_ForExit:
                {
                    print("准备退出游戏....");
                    Application.Quit();
                    break;
                }
        }
    }

    private void GameInvokeNewScene(string scene)
    {
        this.GameMenu_NextScene = scene;
        if (!IsInvoking("LoadGameScene"))
        {
            InvokeRepeating("LoadGameScene", 0f, 0.2f);
        }
    }

    //出去，向上
    public void GameSettingGroupOut(GameObject obj, float time)
    {
   //    iTween.MoveTo(obj, GameSettingPosOut, time);
        iTween.MoveTo(obj, GameSettingPosBottom, time);
    }

    //进来，向下
    public void GameSettingGroupIn(GameObject obj, float time)
    {

     //   iTween.MoveTo(obj, GameSettingPosIn, time);
        obj.SetActive(false);
        //先移动到上方
//        iTween.MoveTo(obj, GameSettingPosTop, time);
        iTween.MoveFrom(obj, GameSettingPosTop, time);

        obj.SetActive(true);
        //再掉下来
        iTween.MoveTo(obj, GameSettingPosMiddle, time);

    }


    void LoadGameScene()
    {
        print("LoadGameScene   ......");
        print("level_text = " + GameMenu_NextScene);
        if (Application.CanStreamedLevelBeLoaded(GameMenu_NextScene))
        {
            print("loading....");
            Application.LoadLevel(GameMenu_NextScene);
        }
        else
            print("preparing....");
    }
}
