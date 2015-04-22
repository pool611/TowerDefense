using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Game1LevelScript : LGameData {
    /*
     * 这一部分，是要在脚本外挂上的变量
     * */
    //关卡选项的游戏物体组
    public GameObject[] GameLevelGroups;
//    public string[] GamePlayerScenes;
//    public LGameControl control;
    public Button[] LMapScenes;
    public Text LevelMsg;

    //游戏关卡数
    private int GameLevelMax = 2;
    private int GameLevelMin = 0;

    //下一个要加载的场景名称
    private string GameLevel_NextSceneName;


    //玩家玩到的关卡，是固定的，用PlayerPrefabs读取
    //根据这个参数判断游戏是否已经解锁
    private int GameLevelUnlocked;



    // Use this for initialization
    void Start()
    {

		print ("GameLevelScript    onStart");
        setListener();
        Init();
    }


    private void setListener()
    {
        LMapScenes[0].onClick.AddListener(delegate() { LGameLevelEnter(0); });
        LMapScenes[1].onClick.AddListener(delegate() { LGameLevelEnter(1); });
        LMapScenes[2].onClick.AddListener(delegate() { LGameLevelEnter(2); });
        LMapScenes[3].onClick.AddListener(delegate() { LGameLevelEnter(3); });
    }

    // Update is called once per frame
    void Update()
    {
    }

    void Init()
    {
        LevelMsg.text = "";
        GameLevelMin = 0;
        GameLevelMax = GameLevelGroups.Length - 1;

        PlayerPrefs.SetInt(PlayerPrefs_LevelTotal, GameLevelGroups.Length);
 //       PlayerPrefs.SetInt(PlayerPrefs_LevelUnlocked, 0);

        //GameLevel		游戏关卡保存
//		PlayerPrefs.SetInt (PlayerPrefs_LevelUnlocked,0);
        GameLevelUnlocked = PlayerPrefs.GetInt(PlayerPrefs_LevelUnlocked);

//		print ("最大解锁关卡："+GameLevelUnlocked);
  //      print("读取到的GameLevelUnlocked 最大解锁关卡 = " + GameLevelUnlocked);
 //       print("当前游戏关卡GameLevelCurrent：" + GameLevelCurrent);

        for (int i = 0; i < IdLevelSize; i++)
        {

			UnityEngine.UI.Image img_lock;
			UnityEngine.UI.Image img_unlock;
			img_lock = GameLevelGroups[i].transform.Find("Lock").GetComponent<UnityEngine.UI.Image>();
			img_unlock = GameLevelGroups[i].transform.Find("UnLock").GetComponent<UnityEngine.UI.Image>();

            if (i <= GameLevelUnlocked)
            {
                img_lock.transform.Translate(0, Screen.height * 2, 0);

            }
            else
            {
                img_unlock.transform.Translate(0, Screen.height * 2, 0);
            }
        }
    }


    void OnGUI()
    {
    }


    public void LGameLevelEnter(int which)
    {
//        print("打开游戏关卡"+which+" 已解锁关卡"+GameLevelUnlocked);
        if (which > GameLevelUnlocked)
        {
  //          print("哎呦这一关还没解锁啦");


            LevelMsg.text = "哎呀，这一关还没有解锁呢";
            StartCoroutine("WaitAndFadeOut");

        }
        else
        {
            switch (which)
            {
                case IdLevel1:
                    {
                        GameLevel_NextSceneName = StrLevel1;
                        GameInvokeNewScene(GameLevel_NextSceneName);
                        break;
                    }
                case IdLevel2:
                    {
                        GameLevel_NextSceneName = StrLevel2;
                        GameInvokeNewScene(GameLevel_NextSceneName);
                        break;
                    }
                case IdLevel3:
                    {
                        GameLevel_NextSceneName = StrLevel3;
                        GameInvokeNewScene(GameLevel_NextSceneName);
                        break;
                    }
                case IdLevel4:
                    {
                        GameLevel_NextSceneName = StrLevel4;
                        GameInvokeNewScene(GameLevel_NextSceneName);
                        break;
                    }
            }
            PlayerPrefs.SetInt(PlayerPrefs_LevelCurrent, which);
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
        if (Application.CanStreamedLevelBeLoaded(GameLevel_NextSceneName))
        {
            Application.LoadLevel(GameLevel_NextSceneName);
        }
    }

    IEnumerator WaitAndFadeOut()
    {
        yield return new WaitForSeconds(5f);
        LevelMsg.text = "";

    }



}
