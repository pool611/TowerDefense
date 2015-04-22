/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//SV (short for static variables) stores all mask codes and some public static variables
public class SV : MonoBehaviour
{
    //bit shift masks are values used by ray casts
    //and layers are used e.g. by collision detection
    public static int gridLayer; //grid layer index
    public static int gridMask;  //grid bit shift mask
    public static int towerLayer; //tower layer index
    public static int towerMask;  //tower bit shift mask
    public static int worldLayer; //world layer index
    public static int worldMask; //world limit bit shift mask
    public static int enemyMask; //enemy bit shift mask
    public static int enemyLayer; //enemy layer index

    public static Vector3 outOfView = new Vector3(0, -300, 0); //out of view position for new towers or powerups
    public static GameObject selection;     //floating tower gameobject - bought or selected tower we want to place
    public static GameObject gridSelection;	//currently selected grid gameobject in BuildMode Grid
    public static GameObject powerUpIndicator; //range/radius indicator for currently selected powerup
    public static SelfControl control;  //SelfControl script of the tower that we control (only one at the same time)
    public static bool showUpgrade; //enable/disable visibility of the 'tower purchase tooltip'
    public static bool showExit;	//enable/disable visibility of the 'exit game menu'


    void Awake()
    {
        Reset();
        SetMasks();
    }


    //create bit shift codes for raycasts,
    //set LayerMasks for mouse detection ray casts
    public static void SetMasks()
    {
        enemyLayer = LayerMask.NameToLayer("Enemies"); //convert layer Enemies to layer number
        gridLayer = LayerMask.NameToLayer("Grid");  //convert layer Grid to layer number
        towerLayer = LayerMask.NameToLayer("Tower"); //convert layer Tower to layer number
        worldLayer = LayerMask.NameToLayer("WorldLimit"); //convert layer WorldLimit to layer number

        gridMask = 1 << LayerMask.NameToLayer("Grid"); // cast ray only against layer Grid (10)
        towerMask = 1 << LayerMask.NameToLayer("Tower"); // cast ray only against layer Tower (11)
        worldMask = 1 << LayerMask.NameToLayer("WorldLimit"); // cast ray only against layer WorldLimit (13)
        enemyMask = 1 << LayerMask.NameToLayer("Enemies"); // cast ray only against layer Enemies (8)
    }


    //reset static variables for a new game so they don't get cached on scene change
    public static void Reset()
    {    
        selection = null;
		gridSelection = null;
        powerUpIndicator = null;
        control = null;
        showUpgrade = false;
        showExit = false;
    }
}