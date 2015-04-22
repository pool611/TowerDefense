using UnityEngine;
using System.Collections;
using UnityEngine.UI;



public class LGameLevelScript : MonoBehaviour
{
    /*
     * 这一部分，是要在脚本外挂上的变量
     * */
    //关卡选项的游戏物体组
    public GameObject[] GameLevelGroups;
    public string[] GamePlayerScenes;
    
    /*
     * 这一部分是定义全部的const对象
     * */
    private const int GameLevelSlide_LAST = 1;
    private const int GameLevelSlide_NEXT = 2;
    private const int GameLevelClick_BackMenu = 1;
    private const int GameLevelClick_GameEntry = 2;

    private const string GameLevelClick_NextBackMenu = "LGameMenu";
    private const string GameLevelClick_NextEntry = "Game1";



    private const string PlayerPrefs_LevelTotal = "GameLevelTotal";
    private const string PlayerPrefs_LevelUnlocked = "GameLevelUnlocked";
    private const string PlayerPrefs_LevelCurrent = "GameLevelCurrent";

    
    //游戏关卡数
    private int GameLevelMax = 2;
    private int GameLevelMin = 0;

    //下一个要加载的场景名称
    private string GameLevel_NextSceneName;

    //移动的屏内和屏外位置
    private Vector3 PositionIn;
    private Vector3 PositionOutLeft;
    private Vector3 PositionOutRight;

    //鼠标按下、拖拽、抬起的三个Position位置
    //Temp是做拖拽时使用的临时变量
    private Vector2 MousePosTemp;
    private Vector2 MousePosFirst;
    private Vector2 MousePosSecond;
    private Vector2 MousePosThird;

    //鼠标按下-抬起的距离
    private float MouseMoveDistanceX;

    //玩家玩到的关卡，是固定的，用PlayerPrefabs读取
    //根据这个参数判断游戏是否已经解锁
    private int GameLevelUnlocked;

    //游戏刚开始时，游戏关卡都是第一个，从0开始计时
    private int GameLevelCurrent = 0;

    //移动选项要求的拖拽长度最小值
    private float ScreenMinLimitX;

    private float GameLevelSlideTime = 0.5f;

    // Use this for initialization
    void Start()
    {


        PositionIn = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        PositionOutLeft = new Vector3(0 - Screen.width / 2, Screen.height / 2, 0);
        PositionOutRight = new Vector3(Screen.width * 2, Screen.height / 2, 0);

        ScreenMinLimitX = Screen.width / 5;
        GameLevelMin = 0;
        GameLevelMax = GameLevelGroups.Length - 1;

        GameLevelCurrent = 0;
         PlayerPrefs.SetInt(PlayerPrefs_LevelTotal,GameLevelGroups.Length);

        //unlock sences
        PlayerPrefs.SetInt(PlayerPrefs_LevelUnlocked,3);

        //GameLevel		游戏关卡保存
        GameLevelUnlocked = PlayerPrefs.GetInt(PlayerPrefs_LevelUnlocked);
        print("读取到的GameLevelUnlocked 最大解锁关卡 = " + GameLevelUnlocked);

        GameLevelGroupIn(GameLevelGroups[GameLevelCurrent],0, 0f);

        print("当前游戏关卡GameLevelCurrent：" + GameLevelCurrent);

        for (int i = 0; i <= GameLevelMax; i++)
        {
			UnityEngine.UI.Image img_lock;
			UnityEngine.UI.Image img_unlock;
            Button bt;
            
			img_lock = GameLevelGroups[i].transform.Find("Lock").GetComponent<UnityEngine.UI.Image>();
			img_unlock = GameLevelGroups[i].transform.Find("UnLock").GetComponent<UnityEngine.UI.Image>();
            bt = GameLevelGroups[i].transform.Find("Button").GetComponent<Button>();
            
            bt.onClick.AddListener(delegate()
            {
                this.LGameLevelEnter();
            });


            if (i <= GameLevelUnlocked)
            {
                //                print(i+"关卡解锁");
                bt.enabled = true;
                img_lock.transform.Translate(0, Screen.height * 2, 0);

            }
            else
            {
                //              print(i+"关卡锁定");
                bt.enabled = false;
                img_unlock.transform.Translate(0, Screen.height * 2, 0);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }


    void OnGUI()
    {
        switch (Event.current.type)
        {
            case EventType.mouseDown:
                {
                    MousePosFirst = Event.current.mousePosition;
                    MousePosTemp = Event.current.mousePosition;
                    break;
                }
            case EventType.mouseDrag:
                {
                    MousePosSecond = Event.current.mousePosition;
                    float tempDistance = MousePosSecond.x - MousePosTemp.x;
                    GameLevelGroups[GameLevelCurrent].transform.Translate(tempDistance, 0, 0);
                    MousePosTemp = Event.current.mousePosition;
                    break;
                }
            case EventType.mouseUp:
                {
                    MousePosThird = Event.current.mousePosition;
                    //计算按下-抬起移动的X方向距离
                    MouseMoveDistanceX = MousePosThird.x - MousePosFirst.x;
                    //判断左右：向左滑了，那么关卡组要向左移动；否则向右滑动
                    if (MouseMoveDistanceX > 0)
                    {
                        GameLevelSlide(GameLevelSlide_LAST);
                    }
                    else
                    {
                        GameLevelSlide(GameLevelSlide_NEXT);
                    }
                    break;
                }
        }

    }


    public void LGameLevelEnter()
    {
        print("开始游戏");
        //判断当前选择的关卡是否已经解锁，GameLevelCurrent和GameLevelPlay比较
        if (GameLevelCurrent <= GameLevelUnlocked)
        {
            print("此关卡已解锁 " + GamePlayerScenes[GameLevelCurrent]);
            GameLevel_NextSceneName = GamePlayerScenes[GameLevelCurrent];
            GameInvokeNewScene(GameLevel_NextSceneName);


			PlayerPrefs.SetInt(PlayerPrefs_LevelCurrent,GameLevelCurrent);
        }
        else
        {
            print("此关卡未解锁");
        }
    }

    /*
     * 单击按钮响应：
     * 返回主菜单
     * 开始游戏
     * */

    public void LGameLevelClick(int i)
    {
        switch(i)
        {
            case GameLevelClick_BackMenu: {
                print("返回主菜单");
                GameInvokeNewScene(GameLevelClick_NextBackMenu);
                break; }
        }
    }

    /*
     * 调用新的场景
     * */
    private void GameInvokeNewScene(string scene)
    {
        GameLevel_NextSceneName = scene;
        if (!IsInvoking("LoadGameScene"))
        {
            InvokeRepeating("LoadGameScene", 0f, 0.2f);
        }
    }

    /*
     * 加载新的场景
     * */
    void LoadGameScene()
    {
        print("LoadGameScene   ......");
        print("level_text = " + GameLevel_NextSceneName);
        if (Application.CanStreamedLevelBeLoaded(GameLevel_NextSceneName))
        {
            print("loading....");
            Application.LoadLevel(GameLevel_NextSceneName);
        }
        else
            print("preparing....");
    }



    /*
     * void GameLevelSlide(int direction) 
     * 左右滑动显示关卡选项列表
     * 若可以移动则消掉当前关卡页
     * 下一个关卡页移到屏幕中间
     * 否则当前关卡页重新回到屏幕中间
     * */
    private void GameLevelSlide(int direction)
    {
        switch (direction)
        {
            case GameLevelSlide_LAST:
                {
                    //可以移动
                    if (Mathf.Abs(MouseMoveDistanceX) > ScreenMinLimitX)
                    {
                        if (GameLevelCurrent <= GameLevelMin)
                        {
                            print("不好意思啊，这已经是第一个关卡啦");
                            GameLevelGroupIn(GameLevelGroups[GameLevelCurrent],0, GameLevelSlideTime);
                        }
                        else
                        {
                            GameLevelGroupOutRight(GameLevelGroups[GameLevelCurrent], GameLevelSlideTime);
                            GameLevelCurrent--;
                            GameLevelGroupIn(GameLevelGroups[GameLevelCurrent],0, GameLevelSlideTime);
                        }
                    }
                    else
                    {
                        GameLevelGroupIn(GameLevelGroups[GameLevelCurrent],0, GameLevelSlideTime);
                    }
                    break;
                }
            case GameLevelSlide_NEXT:
                {
                    if (Mathf.Abs(MouseMoveDistanceX) > ScreenMinLimitX)
                    {
                        if (GameLevelCurrent >= GameLevelMax)
                        {
                            print("啊哦，这已经是最后一个关卡了");
                            GameLevelGroupIn(GameLevelGroups[GameLevelCurrent],1, GameLevelSlideTime);
                        }
                        else
                        {
                            GameLevelGroupOutLeft(GameLevelGroups[GameLevelCurrent], GameLevelSlideTime);
                            GameLevelCurrent++;
                            GameLevelGroupIn(GameLevelGroups[GameLevelCurrent],1, GameLevelSlideTime);
                        }
                    }
                    else
                    {
                        GameLevelGroupIn(GameLevelGroups[GameLevelCurrent],1, GameLevelSlideTime);
                    }
                    break;
                }
        }
    }


    /*
     * 拖动关卡选项页到屏幕内外
     * 1、到外面，向左
     * 2、到外面，向右
     * 3、到中间
     * */

    //拖到外面，向左
    private void GameLevelGroupOutLeft(GameObject obj, float time)
    {
        iTween.MoveTo(obj, PositionOutRight, 0f);
        iTween.MoveTo(obj, PositionOutLeft, time);
    }

    //拖到外面，向右
    private void GameLevelGroupOutRight(GameObject obj, float time)
    {
        iTween.MoveTo(obj, PositionOutLeft, 0f);
        iTween.MoveTo(obj, PositionOutRight, time);
    }

    //拖到屏幕中间
    private void GameLevelGroupIn(GameObject obj,int direction, float time)
    {
        if (direction == 0)
        {
            iTween.MoveTo(obj, PositionOutLeft, 0f);
            print("从左边进");
        }
        else
        {
            iTween.MoveTo(obj, PositionOutRight, 0f);
            print("从右边进");
        }
        iTween.MoveTo(obj, PositionIn, time);
    }

}

