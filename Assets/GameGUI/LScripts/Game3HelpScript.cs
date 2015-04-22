using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Game3HelpScript : LGameData {

    public Button btnAboutUs;
    public Button btnAboutMonster;
    public Button btnAboutTower;

    public GameObject gobjAboutUs;
    public GameObject gobjAboutMonster;
    public GameObject gobjAboutTower;
    

    public Text DescribeUs;
    public Text DescribeMonster;
    public Text DescribeTower;
    public Text HelpMsg;

    private string StrUs;
    private string StrMonster;
    private string StrTower;

    public AudioSource AudioTypeWriter;

    private float time = (float)0.2;

    private void Show(int which)
    {
        switch (which)
        {
            case IdBtnAboutUs: {
                gobjAboutUs.SetActive(true);
                gobjAboutMonster.SetActive(false);
                gobjAboutTower.SetActive(false);
                HelpMsg.text = "关于我们";
  //              StartCoroutine("PrintUs");

                break; }
            case IdBtnAboutMonster: {
                gobjAboutUs.SetActive(false);
                gobjAboutMonster.SetActive(true);
                gobjAboutTower.SetActive(false);
    //            StartCoroutine("PrintMonster");
                HelpMsg.text = "关于小怪";
                break;}
            case IdBtnAboutTower: {
                gobjAboutUs.SetActive(false);
                gobjAboutMonster.SetActive(false);
                gobjAboutTower.SetActive(true);
                HelpMsg.text = "关于防御塔";
      //          StartCoroutine("PrintTower");

                break; }

        }
    }

	// Use this for initialization
	void Start () {

    //    initStr();
        gobjAboutUs.SetActive(false);
        gobjAboutMonster.SetActive(false);
        gobjAboutTower.SetActive(false);
        HelpMsg.text = "";


        Show(IdBtnAboutUs);

  //      print("这是游戏帮助界面");
        btnAboutUs.onClick.AddListener(delegate() { this.LAboutClick(IdBtnAboutUs); });
        btnAboutMonster.onClick.AddListener(delegate() { this.LAboutClick(IdBtnAboutMonster); });
        btnAboutTower.onClick.AddListener(delegate() { this.LAboutClick(IdBtnAboutTower); });
	}




	
	// Update is called once per frame
	void Update () {
	
	}


    private void LAboutClick(int which)
    {
        Show(which);


    }

    private void initStr()
    {
        StrUs = DescribeUs.text;
        DescribeUs.text = "";
        StrTower = DescribeTower.text;
        DescribeTower.text = "";
        StrMonster = DescribeMonster.text;
        DescribeMonster.text = "";
    }

    IEnumerator PrintUs()
    {
        DescribeUs.fontSize = 14;
        for (int i = 0; i < StrUs.Length; i++)
        {
            DescribeUs.text += StrUs[i];
            yield return new WaitForSeconds(time);
        }
        AudioTypeWriter.Stop();
    }

    IEnumerator PrintTower()
    {
        DescribeTower.fontSize = 14;
        for (int i = 0; i < StrTower.Length; i++)
        {
            DescribeTower.text += StrTower[i];
            yield return new WaitForSeconds(time);
        }
        AudioTypeWriter.Stop();
    }

    IEnumerator PrintMonster()
    {
        DescribeMonster.fontSize = 14;
        for (int i = 0; i < StrMonster.Length; i++)
        {
            DescribeMonster.text += StrMonster[i];
            yield return new WaitForSeconds(time);
        }
        AudioTypeWriter.Stop();
    }
}
