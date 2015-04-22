using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Game2SettingScript : LGameData {

    public Slider LVolumeSlider;
    public Toggle LSilence;
    private float mvolume=1;
    public AudioSource AudioBGM;
    private bool isMute=false;

    //滑动器数据改变监听
    public void GameSettingVolumeChanged(float volume)
    {
        mvolume = volume;
    }

    //是否静音
    public void GameSettingSilence(bool isOn)
    {
        if (isOn)
        {
            isMute = true;
        }
        else
        {
            isMute = false;
        }


    }

	// Use this for initialization
	void Start () {
        AudioBGM.mute = false;

	}
	
	// Update is called once per frame
	void Update () {

	}

    void OnGUI()
    {
//        print("volume=" + mvolume + "   mute = " + isMute);
        AudioBGM.volume = mvolume;
        AudioBGM.mute = isMute;
    }
}
