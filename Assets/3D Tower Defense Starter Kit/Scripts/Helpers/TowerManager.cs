/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//TowerManager.cs stores all tower-relevant info, such as names,
//prefabs and corresponding upgrade scripts.
public class TowerManager : MonoBehaviour
{
    //script reference
    public static TowerManager instance;
    //a list for tower names
    [HideInInspector]    
    public List<string> towerNames = new List<string>();
    //tower prefabs to instantiate when buying
    [HideInInspector]
    public List<GameObject> towerPrefabs = new List<GameObject>();
    //how much we will lose in percentage if we sell a bought tower
    [HideInInspector]  
    public int sellLoss;
    //lists to store tower properties
    //for displaying the tooltip without instantiation
    [HideInInspector] 
    public List<Upgrade> towerUpgrade = new List<Upgrade>();
    [HideInInspector]
    public List<TowerBase> towerBase = new List<TowerBase>();


    void Start()
    {
        instance = this;
        //here we need to instantiate every tower so we have access to their basic properties at any time
        //and are able to display their stats, e.g. on mouse over button events
        //(we can't get their properties without instantiation,
        //and its inefficient to instantiate and destroy them every time we need them)
        for (int i = 0; i < towerNames.Count; i++)
        {
            if (towerPrefabs[i] == null)
            {
                Debug.LogWarning("Prefab not set for tower " + (i+1) + " in TowerManager!");
                return;
            }

            //instantiate tower at a non-visible position
            GameObject tower = (GameObject)Instantiate(towerPrefabs[i], SV.outOfView, Quaternion.identity);
            //rename tower clone to the name given in list "towerNames" ( typed in via inspector)
            tower.name = towerNames[i];
            //parent tower to this gameobject which acts as tower container
            tower.transform.parent = transform;
            //add base properties to list above
            TowerBase tBase = tower.GetComponentInChildren<TowerBase>();
            towerBase.Add(tBase);
            //add upgrade properties to list above
            Upgrade upg = tower.GetComponentInChildren<Upgrade>();
            towerUpgrade.Add(upg);
            tBase.upgrade = upg;
            //deactivate tower and all of its children, so it does not affect our game world
            tower.SetActive(false);
        }
    }
}
