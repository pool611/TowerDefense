/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;

//simple GUI script that maps buttons to load a scene
public class Intro : MonoBehaviour
{
    //if a button gets clicked, load the scene assigned to the button
    public void LoadScene(string sceneName)
    {
        Application.LoadLevel(sceneName);
    }
}
