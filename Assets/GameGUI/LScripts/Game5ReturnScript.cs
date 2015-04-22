using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Game5ReturnScript : LGameData {

    public LGameControl control;
    public Button btnReturn;

	void Start () {

 //       print("我是返回啊。。。。。。。。。。。。。");
//        print("GameMenu访问。。。"+GameObjectGroup[IdGameMenu].name);
        btnReturn.onClick.AddListener(delegate() { control.LGameClick(IdBtnReturn); });
	}
	
	// Update is called once per frame
	void Update () {
	
	}

}
