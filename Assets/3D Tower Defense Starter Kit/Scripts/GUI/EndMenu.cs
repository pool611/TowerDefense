/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

//Game Over Menu ( lost and won )
public class EndMenu : MonoBehaviour
{
    //SOUND EFFECTS
    public AudioClip gameWon;
    public AudioClip gameLost;
    public AudioClip endMusic;

    //label for stats of the played game
    public Text stats;
    //button to return to the main menu
    public GameObject btn_mainButton;
    //main menu scene name
    public string mainSceneName;

    //projectile to instantiate for some special effects 
    public GameObject impactProj;


    IEnumerator Start()
    {
        if (!GameObject.Find("Game Manager"))
        {
            Debug.LogWarning("EndMenu.cs can't find any game stats. Cancelling. Have you played the game?");
            yield break;
        }

        //construct scene depending on game state (won or lost)
        //definition by cases
        ConstructScene();

        //wait until sounds are played before starting music
        if (GameHandler.gameOver)
            yield return new WaitForSeconds(gameLost.length);
        else
            yield return new WaitForSeconds(gameWon.length);

        //play background music
        AudioManager.Play(endMusic);
        //start special effect coroutine
        StartCoroutine("Impacts");
    }


    //set stats label with all game information
    void ConstructScene()
    {
        //create new string
        string text = "";
        //start blue text color
        text = "<color=#0101DF>";

        //insert text depending on game state
        if (GameHandler.gameOver)
        {
            //we have lost the game, insert text
            text += "GAME LOST!";
            //destroy the scene gameobject that holds all the gold
            Destroy(GameObject.Find("Content"));
            //play game lost sound
            AudioManager.Play2D(gameLost);
        }
        else
        {
            //we have won the game, insert text
            text += "GAME WON!";
            //destroy the scene gameobject that holds the fire
            Destroy(GameObject.Find("Fire"));
            //play win sound
            AudioManager.Play2D(gameWon);
        }

        //end blue text color, begin black color and insert some line spacings
        text += "</color>\n\n";

        //set text with all game related information, coloring the important parts
        text += "Wave: <color=#FF0000>" + GameHandler.wave + " / " + GameHandler.waveCount + "</color>\n"
                + "Enemies alive: <color=#FF0000>" + GameHandler.enemiesAlive + "</color>\n"
                + "Enemies killed: <color=#FF0000>" + GameHandler.enemiesKilled + "</color>\n"
                + "Gold left: <color=#FF0000>" + GameHandler.gameHealth + "</color>\n"
                + "Resources: <color=#FF0000>" + GameHandler.resources[0] + "</color>";

        //finally set the label text to the constructed text and end black color
        stats.text = text + "[-]";
    }


    public void BackToMain()
    {
       //this button brings us back to the main menu
       //we additionally destroy the GameHandler gameobject which
       //contains all played game data, since it's not needed anymore
       Destroy(GameObject.Find("Game Manager"));
       //load menu scene
       Application.LoadLevel(mainSceneName);
    }


    //spawn a cannon ball projectile every 5 seconds
    IEnumerator Impacts()
    {
        //create a target transform, where the cannon ball should fly to
        Transform target = new GameObject("Target").transform;

        //enter endless loop
        while (true)
        {
            //get a random position in front of the camera
            Vector3 randomPos = new Vector3(Random.Range(-10, 10), -2, Random.Range(-5, 10));
            //set target position to this calculated random position
            target.position = randomPos;

            //get a random position above the camera view to let the projectile spawn there
            Vector3 instPos = new Vector3(Random.Range(-20, 20), 10, Random.Range(-10, 10));
            //instantiate projectile at this random position, make use of the pool manager
            GameObject newProj = PoolManager.Pools["Projectiles"].Spawn(impactProj, instPos, Quaternion.identity);
            //get projectile component of the cannon ball and set its target
            newProj.GetComponent<Projectile>().target = target;

            //wait 5 seconds before spawning another cannon ball
            yield return new WaitForSeconds(5);            
        }
    }
}