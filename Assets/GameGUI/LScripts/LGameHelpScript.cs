using UnityEngine;
using System.Collections;

public class LGameHelpScript : MonoBehaviour {


    //下一个要进入的场景名称
    private string GameMenu_NextScene;
    private const int GameHelpClick_ForBackMenu = 0;

    private const string GameHelpClick_NextSceneMenu="LGameMenu";

    public void LGameHelpClick(int i)
    {
        switch (i)
        {
            case GameHelpClick_ForBackMenu: {

                GameInvokeNewScene(GameHelpClick_NextSceneMenu);
                break; }
        }
    }



	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
    private void GameInvokeNewScene(string scene)
    {
        this.GameMenu_NextScene = scene;

        if (!IsInvoking("LoadGameScene"))
        {
            InvokeRepeating("LoadGameScene", 0f, 0.2f);
        }
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
