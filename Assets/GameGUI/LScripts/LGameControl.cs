using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LGameControl : LGameData {

    public GameObject Game0Menu;
    public GameObject Game1Level;
    public GameObject Game2Setting;
    public GameObject Game3Help;
    public GameObject Game4Over;
    public GameObject Game5Return;
    int i = 0;
    private const int Size = 6;
    protected GameObject[] GameObjectGroup = new GameObject[Size];


    protected Vector3 GameObjectPosMiddle = new Vector3(Screen.width / 2, Screen.height / 2, 0);
    protected Vector3 GameObjectPosTop = new Vector3(Screen.width / 2, Screen.height * 2, 0);
    protected Vector3 GameObjectPosBottom = new Vector3(Screen.width / 2, -Screen.height * 2, 0);
    protected Vector3 GameObjectPosLeft = new Vector3(-Screen.width * 2, Screen.height / 2, 0);
    protected Vector3 GameObjectPosRight = new Vector3(Screen.width * 2, Screen.height / 2, 0);



    void Start()
    {
        Init();
        LLoadGameObject(IdGameMenu);
    }



    public void Init()
    {
        GameObjectGroup[IdGameMenu] = this.transform.Find("GameMenu").gameObject;
        GameObjectGroup[IdGameLevel] = this.transform.Find("GameLevel").gameObject;
        GameObjectGroup[IdGameSetting] = this.transform.Find("GameSetting").gameObject;
        GameObjectGroup[IdGameHelp] = this.transform.Find("GameHelp").gameObject;
        GameObjectGroup[IdGameReturn] = this.transform.Find("GameReturn").gameObject;
        GameObjectGroup[IdGameOver] = this.transform.Find("GameOver").gameObject;
    }



    //只显示某个组件组
    public void LLoadGameObject(int which)
    {

		print("随便打印出来");

        GameObjectGroup[IdGameReturn].SetActive(true);
        int i = 0;
        while (i < GameObjectGroup.Length)
        {
            if(i!=IdGameReturn)
                //which代表的组件组进入，其他的迅速移除
                if (i == which)//遇到要进来的组件
                {
                    if (i == IdGameMenu)
                    {
                        GameObjectGroup[i].SetActive(true);
                        LGameObjectInFromRight(GameObjectGroup[i], LGameData.TimeSlow);
                        GameObjectGroup[IdGameReturn].SetActive(false);
                    }
                    else
                    { 
                           GameObjectGroup[i].SetActive(true);
                        LGameObjectInFromLeft(GameObjectGroup[i], LGameData.TimeSlow);
                        GameObjectGroup[IdGameReturn].SetActive(true);
                    }
                }
                else//这是要出去的组件
                {
                    if (i == IdGameMenu)
                    {
                        GameObjectGroup[i].SetActive(false);
                        LGameObjectOutToRight(GameObjectGroup[i], LGameData.TimeFast);
                    }
                    else
                    { 
                       GameObjectGroup[i].SetActive(false);
                       LGameObjectOutToLeft(GameObjectGroup[i], LGameData.TimeFast);
                    }
                }
            i++;
        }

    }



    public void LGameClick(int which)
    {
        switch (which)
        {
            case IdBtnExit: { print("单击事件：" + which+"退出，关闭程序"); Application.Quit(); break; }
            case IdBtnHelp: { print("单击事件：" + which+"帮助，打开帮助页面"); LLoadGameObject(IdGameHelp); break; }
            case IdBtnLevel: { print("单击事件：" + which + "关卡，打开关卡选择页面"); LLoadGameObject(IdGameLevel); break; }
            case IdBtnReturn: { print("单击事件：" + which+"返回，打开主菜单页面"); LLoadGameObject(IdGameMenu); break; }
            case IdBtnSetting: { print("单击事件：" + which+"设置，打开设置页面"); LLoadGameObject(IdGameSetting); break; }
            case IdBtnReplay: { print("单击事件：" + which+"重玩，这个我没辙啊 你自己再写吧"); break; }
        }
         
    }
	// Use this for initialization
    //从左边出去
    public void LGameObjectOutToLeft(GameObject obj, float time)
    {
        iTween.MoveTo(obj, new Vector3(-Screen.width * 2, obj.transform.position.y, 0), time);
        obj.SetActive(false);
    }
    //从左边进来
    public void LGameObjectInFromLeft(GameObject obj, float time)
    {
        obj.SetActive(false);
        //先移动到左边
        iTween.MoveFrom(obj, new Vector3(-Screen.width * 2, obj.transform.position.y, 0), time);
        obj.SetActive(true);
        //再移动到中间
        iTween.MoveTo(obj, new Vector3(Screen.width / 2, obj.transform.position.y, 0), time);
    }


    public void LGameObjectInFromRight(GameObject obj, float time)
    {
        obj.SetActive(false);
        //先移动到右边
 //       iTween.MoveFrom(obj, new Vector3(Screen.width * 2, obj.transform.position.y, 0), time);
        obj.SetActive(true);
        //再移到中间
        iTween.MoveTo(obj, new Vector3(Screen.width / 2, obj.transform.position.y, 0), time);
    }

    //从右边出去
    public void LGameObjectOutToRight(GameObject obj, float time)
    {
        //        iTween.MoveTo(obj, GameObjectPosRight, time);
        iTween.MoveTo(obj, new Vector3(Screen.width * 2, obj.transform.position.y, 0), time);
        obj.SetActive(false);
    }

    /*
    //从下面出去
    public void LGameObjectOutToBottom(GameObject obj, float time)
    {
        //直接掉到屏幕下面
//        iTween.MoveTo(obj, GameObjectPosBottom, time);
        iTween.MoveTo(obj, new Vector3(obj.transform.position.x,-Screen.height*2,0), time);

        obj.SetActive(false);
    }




    //从上面进来
    public void LGameObjectInFromTop(GameObject obj, float time)
    {
        obj.SetActive(false);
        //先移动到上方
//        iTween.MoveFrom(obj, GameObjectPosTop, time);
        iTween.MoveFrom(obj,new Vector3(obj.transform.position.x,Screen.height*2,0),time);
        obj.SetActive(true);
        //再掉下来
//        iTween.MoveTo(obj, GameObjectPosMiddle, time);
        iTween.MoveTo(obj, new Vector3(obj.transform.position.x, Screen.height / 2, 0), time);
    }




    //从当前位置到中间
    public void LGameObjectInFromCurrent(GameObject obj, float time)
    {
        obj.SetActive(true);
        //        iTween.MoveTo(obj,GameObjectPosMiddle,time);
        iTween.MoveTo(obj, new Vector3(Screen.width / 2, obj.transform.position.y, 0), time);
    }
    */
}
