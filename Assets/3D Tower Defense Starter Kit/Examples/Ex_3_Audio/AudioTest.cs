/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;


//audio test class, part of example scene "Example_Audio"
public class AudioTest : MonoBehaviour
{
    public AudioClip music1;
    public AudioClip audio1;
    public AudioClip audio2;
    public AudioClip audio3;


    void OnGUI()
    {
        if (GUI.Button(new Rect(0,0, 120, 30), "Play Music"))
        {
            AudioManager.Play(music1);
        }

        if (GUI.Button(new Rect(120, 0, 120, 30), "Stop Music"))
        {
            AudioManager.Stop();
        }

        if (GUI.Button(new Rect(0, 50, 120, 30), "Play 3D"))
        {
            AudioManager.Play(audio1,
                              Camera.main.transform.position + new Vector3(Random.Range(-30, 30),0,0));
        }

        if (GUI.Button(new Rect(0, 100, 120, 30), "Play 3D + pitch"))
        {
            AudioManager.Play(audio2,
                              Camera.main.transform.position + new Vector3(Random.Range(-30, 30), 0, 0),
                              Random.Range(0.5f, 1.5f));
        }

        if (GUI.Button(new Rect(0, 150, 120, 30), "Play 2D"))
        {
            AudioManager.Play2D(audio3);
        }
    }
}
