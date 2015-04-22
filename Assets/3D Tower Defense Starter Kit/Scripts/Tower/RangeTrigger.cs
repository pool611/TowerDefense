/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//RangeTrigger.cs is the basic Enemy Detection interface of a tower
public class RangeTrigger : MonoBehaviour
{
    //tower base script reference
    private TowerBase towerScript;  

    void Start()
    {
        //get tower base script reference
        towerScript = transform.parent.gameObject.GetComponentInChildren<TowerBase>();
    }


    //something has passed our area of interest / triggered our collider
    //(collider range is determined by radius variable and therefore by Upgrade.cs)
    void OnTriggerEnter(Collider col)
    {
        //get collided object
        GameObject colGO = col.gameObject;
		
        //skip it the object is not an enemy, or it died already
        if (colGO.layer != SV.enemyLayer || !colGO.activeInHierarchy || PoolManager.Props[colGO.name].health <= 0)
            return;

        //here we check against attackable enemyTypes: whether we can attack air and ground targets
        //in case it is an potential enemy and its tag is in line with our possible targets, continue
        if (towerScript.myTargets == TowerBase.EnemyType.Both
            || colGO.tag == towerScript.myTargets.ToString())
        {
            //enemy spotted, add to enemies which are in range - list of TowerBase.cs
            towerScript.inRange.Add(colGO);
            //also add this tower reference to the collided enemy,
            //so on death it has this tower cached and sends important messages to it (removal)
            colGO.SendMessage("AddTower", towerScript);
        }

        //if an enemy has passed our range and we have an attackable target,
        //start invoking TowerBase's CheckRange() method
        //(StartInvoke() checks if it is running already, so it does not run twice)
        if (towerScript.inRange.Count == 1)
        {
            towerScript.StartInvoke(0f);
        }
    }


    //something has left our area of interest / collider range
    void OnTriggerExit(Collider col)
    {
        //get collided object
        GameObject colGO = col.gameObject;

        //we don't need to check the enemy type again,
        //we look up our inRange list instead and search this gameobject 
        if (towerScript.inRange.Contains(colGO))
        {
            //collided object was added before and recognized as enemy
            //enemy left our radius, remove from in range list
            towerScript.inRange.Remove(colGO);
            //and on the other side, remove our tower from enemy dictionary
            colGO.SendMessage("RemoveTower", towerScript);
        }
    }
}
