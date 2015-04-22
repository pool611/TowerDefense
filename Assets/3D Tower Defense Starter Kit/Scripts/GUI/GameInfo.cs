/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

//GameInfo.cs displays some general GUI data such as wave count,
//defeated enemies, health, resources and the wave timer and
//controls the visibility of the 'start next wave'-button
public class GameInfo : MonoBehaviour
{
    //access to wave status
    public WaveManager waveScript;

    //launch waves button
    public GameObject btn_startWave;

    //all game state information labels go here
    //wave index indicator label
    public Text lbl_wave;
    //enemies alive count label
    public Text lbl_alive;
    //enemies killed count label
    public Text lbl_killed;
    //gold (health) count label
    public Text lbl_gold;
    //time until next wave starts label
    public Text lbl_waveTimer;
    //resources count label
    public Text[] lbl_resources;

    void Start()
    {
        //print error if waveScript is not set in editor
        if (waveScript == null)
            Debug.LogWarning("WaveManager not set, no Game Info GUI possible.");
    }


    void Update()
    {
        //check current wave state
        bool nextWave = CheckWave();

        //enable/disable start wave button based on returned boolean
        if (btn_startWave.activeInHierarchy != nextWave)
            btn_startWave.SetActive(nextWave);

        //set game information labels every frame to always meet the current state
        lbl_wave.text = GameHandler.wave + " / " + GameHandler.waveCount;
        lbl_alive.text = GameHandler.enemiesAlive.ToString();
        lbl_killed.text = GameHandler.enemiesKilled.ToString();

        lbl_gold.text = GameHandler.gameHealth.ToString();

        //set timer until next wave starts, but ignore zero as value
        if (waveScript.secTillWave == 0)
            lbl_waveTimer.text = "";
        else
            lbl_waveTimer.text = waveScript.secTillWave.ToString();

        //update multiple resources
        for (int i = 0; i < lbl_resources.Length; i++)
        {
            lbl_resources[i].text = GameHandler.resources[i].ToString();
        }
    }


    //investigate current wave state
    //and return corresponding boolean value:
    //false: starting the next wave is currently not possible. start next wave button is disabled.
    //true: the last wave fully run through, start next wave button is enabled.
    private bool CheckWave()
    {
        //don't show the start wave button if the exit menu is active or we lost
        if (SV.showExit || GameHandler.gameHealth <= 0)
            return false;

		//we skip further checks if we play on WaveMode normal
        //and the current wave exceeds the actual amount of waves
        if (waveScript.waveMode == WaveManager.WaveMode.normal
            && GameHandler.wave >= int.Parse(GameHandler.waveCount))
            return false;
			
        //this switch handles whether the 'start wave' button gets displayed
        //depending on the chosen wave start option
        switch (waveScript.waveStartOption)
        {
            //on option 'wait for user input' we skip rendering the start button:
            //if the bool 'userInput' of our WaveManager is false
            //userInput gets toggled when all enemies of the current wave are sucessfully spawned  
            case WaveManager.WaveStartOption.userInput:
                if (!waveScript.userInput)
                    return false;
                break;
            //on 'wait till wave is cleared' and 'inverval' we only need to start the first wave,
            //then further waves continue to run without our input and we skip rendering the start button
            case WaveManager.WaveStartOption.waveCleared:
            case WaveManager.WaveStartOption.interval:
                if (GameHandler.wave > 0)
                    return false;
                break;
            //on 'round based' we skip rendering the start button if WaveManager's "CheckStatus" is running
            //this means that the current wave still has enemies alive
            case WaveManager.WaveStartOption.roundBased:
                if (waveScript.IsInvoking("CheckStatus"))
                    return false;
                break;
        }

        //if none of these conditions matched,
        //we return true and therefore enable the 'start next wave'-button
        return true;
    }


    //start next wave, spawn enemies!
    public void NextWave()
    {
        waveScript.StartWaves();
    }

}
