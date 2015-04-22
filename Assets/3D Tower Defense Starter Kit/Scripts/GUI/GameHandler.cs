/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;

//GameHandler.cs stores all relevant game info,
//whether the game is lost or won, how many enemies were killed,
//our gold and health status etc.
public class GameHandler : MonoBehaviour
{
    public static GameHandler instance;    //script reference for coroutine calls
    public static float gameHealth = 1f;   //our health (has to be greater than zero)
    public static float[] resources;  //money left
    public static int enemiesAlive;  //enemies currently alive
    public static int enemiesKilled;    //enemies killed since start
    public static bool gameOver = false;    //toggle game status, won or lost
    public static int wave = 0; //current wave, set by WaveManager.cs
    public static string waveCount = "oo"; //total wave count, set by WaveManager.cs
    public static float maxHealth = 1f; //max starting health
    public float maxGameHealth = 1f;     //health setting in inspector
    public float[] startResources;    //money setting in inspector

    public string nextScene; //next scene to load, allowing multiple scenes
    public string gameOverScene; //game over scene to load when the game is over





    private const string PlayerPrefs_LevelTotal = "GameLevelTotal";
    private const string PlayerPrefs_LevelUnlocked = "GameLevelUnlocked";
    private const string PlayerPrefs_LevelCurrent = "GameLevelCurrent";
    private const string PlayerPrefs_LevelStatus = "GameStatus";

    void Awake()
    {
        //reset static game variables and assign inspector variables to static values
        //(maybe we want to play another round, and static vars always keep their values
        //over scene changes so gamOver could already be set to true or enemiesKilled > 0 etc.)
        instance = this;
        gameHealth = maxHealth = maxGameHealth;
        resources = startResources;
        enemiesAlive = 0;
        enemiesKilled = 0;
        wave = 0;
        waveCount = "oo";
        gameOver = false;

        //invoke "CheckGameState" every second
        InvokeRepeating("CheckGameState", 1f, 1f);
    }

   
    void CheckGameState()
    {
        //constantly check if enemies substracted all our gold/health and game is not over yet
        if (gameHealth <= 0 && !gameOver)
        {
            Debug.Log(" YOU've LOST the Game! ");

            gameHealth = 0;     //GUI should not display gold/health lower than 0, so we set it here
            gameOver = true;    //game is over now, we lost - toggle gameOver to true
            PlayerPrefs.SetString(PlayerPrefs_LevelStatus,"LOST");

            //Load game over scene after few seconds
            StartCoroutine("LoadGameOverScene");
        }
        else if (gameHealth > 0 && gameOver)    //game over but we have gold left ( won )
        {
            Debug.Log(" YOU've WON the Game! ");


            int x = PlayerPrefs.GetInt(PlayerPrefs_LevelUnlocked);
            x++;
            PlayerPrefs.SetInt(PlayerPrefs_LevelUnlocked,x);//多解锁一个关卡

/*            int cur = PlayerPrefs.GetInt(PlayerPrefs_LevelCurrent);
            cur++;
            PlayerPrefs.SetInt(PlayerPrefs_LevelCurrent,cur);
*/
            PlayerPrefs.SetString(PlayerPrefs_LevelStatus, "WIN");

            //game is over now, we won - WaveManager toggled gameOver to true at last wave
            //but we want to play the win sound and toggle gameOver back to false
            //(method LoadGameOverScene checks for gameOver to decide which sound to play)
            gameOver = false;

            //Load game over scene after few seconds
            StartCoroutine("LoadGameOverScene");
        }
    }


    //we killed an enemy, it calls this method
    public static void SetResources(int index, float points)
    {
        //add resources
        resources[index] += points;
    }


    //coroutine for adding resources over time, calls SetResources internally
    public IEnumerator SetResourcesRoutine(PassivePowerUp.PassivePlayerPowerUp.Resources res)
    {
        while (true)
        {
            yield return new WaitForSeconds(res.interval);
            
            for (int i = 0; i < res.value.Length; i++)
            {
                float resDiff = res.value[i];
                if (res.type == TDValue.percentual)
                    resDiff = Mathf.Round(resources[i] * res.value[i] * 100f) / 100f;
                if (res.costType == CostType.intValue)
                    resDiff = (int)resDiff;
                SetResources(i, resDiff);
            }
        }
    }


    //an enemy was killed by a projectile
    public static void EnemyWasKilled()
    {
        //reduce alive count and increase kill count by one 
        enemiesAlive--;
        enemiesKilled++;
    }


    //add health back to the player, eventually used by passive power-ups
    public static void AddHealth(float points)
    {
        gameHealth += points;
        if (gameHealth > maxHealth)
            gameHealth = maxHealth;
    }


    //coroutine for adding health over time, calls AddHealth internally
    public IEnumerator AddHealthRoutine(PassivePowerUp.PassivePlayerPowerUp.Health heal)
    {
        while (true)
        {
            yield return new WaitForSeconds(heal.interval);

            float healthDiff = heal.value;
            if (heal.type == TDValue.percentual)
                healthDiff = (int)(maxHealth * heal.value);
            AddHealth(healthDiff);
        }
    }


    //an enemy has reached its destination
    public static void DealDamage(float dmg)
    {
        //game is already over, do nothing
        if (gameOver) return;
        //else reduce our gold/health
        gameHealth -= dmg;
        //substract one enemy from our alive counter
        //since enemies get removed at their destination 
        enemiesAlive--;
    }


    //load game over scene
    IEnumerator LoadGameOverScene()
    {
        //stop game state check
        CancelInvoke("CheckGameState");

        //if we lost the game, we change the next scene
        //to the game over scene. if you only have one scene
        //nextScene is equal to gameOverScene
        if(gameOver || nextScene == "")
            nextScene = gameOverScene;

        if (nextScene == gameOverScene)
        {
            DontDestroyOnLoad(gameObject);

            //wait few seconds before switching the scene
            yield return new WaitForSeconds(5);

            //unparent this gameobject and set it undestroyable on scene change,
            //to ensure this gameobject gets transported to the next scene and
            //we can display all game relevant stats via our EndMenu.cs
            //(Enemies killed, Gold and Health left etc...)
            transform.parent = null;
        }

        //load GameOver scene
        Application.LoadLevel(nextScene);
    }
}
