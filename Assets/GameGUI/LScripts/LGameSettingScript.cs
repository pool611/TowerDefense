using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LGameSettingScript : MonoBehaviour {

	public Slider LVolumeSlider;
    public Toggle LSilence;


	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	//滑动器数据改变监听
	public void GameSettingVolumeChanged(float volume)
	{
		print ("音量调节 volume  = "+volume);
	}

	//是否静音
	public void GameSettingSilence(bool isOn)
	{
		print ("静音设置  silence = "+isOn);
	}
}
