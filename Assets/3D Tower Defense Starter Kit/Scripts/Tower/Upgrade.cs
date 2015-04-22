/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Upgrade.cs caches all relevant upgrade options
public class Upgrade : MonoBehaviour
{
    //current upgrade level (accessible through code but not shown in inspector)
    [HideInInspector]
    public int curLvl = 0;

    //list of all possible upgrade values defined in its own class
    public List<UpgOptions> options = new List<UpgOptions>(3);

    private SphereCollider sphereCol;   //enemy detection collider of RangeTrigger gameobject
    private Transform rangeIndicator;   //visible RangeIndicator transform

    private TowerBase towerScript;  //towerbase reference


    void Start()
    {
        //don't execute further code when no upgrade options are defined but debug a warning
        if (options.Count == 0)
        {
            Debug.LogWarning("No upgrade options for tower " + gameObject.name +
                             " assigned! This will cause errors at runtime.");
            return;
        }

        //set collider radius to prefab/inspector defined radius
        sphereCol = transform.parent.FindChild("RangeTrigger").gameObject.GetComponent<SphereCollider>();
        sphereCol.radius = options[curLvl].radius;

        //get rangeIndicator gameObject
        rangeIndicator = transform.parent.FindChild("RangeIndicator");
        if(rangeIndicator) RangeChange();  //cause RangeIndicator to adjust its range

        //get towerbase reference
        towerScript = gameObject.GetComponent<TowerBase>();
    }


    //called on every lvl upgrade
    public void LVLchange()
    {
        //increase level
        curLvl++;
        //if a new model on this level is set,
        //then swap it out
        if (options[curLvl].swapModel != null)
        {
            TowerChange();
            return;
        }

        //adjust indicator range
        RangeChange();

        //cancel TowerBase's CheckRange method and start a new one
        //(this causes that this tower promptly reacts to its new damage and delay settings)
        towerScript.CancelInvoke("CheckRange");

        //calculate remaining time when this tower can shoot again
        //so we start the tower AI not until the time comes
        float lastShot = towerScript.lastShot;
        float invokeInSec = options[curLvl].shootDelay + lastShot - Time.time;
        if (invokeInSec < 0) invokeInSec = 0f;

        towerScript.StartInvoke(invokeInSec);
    }


    //adjust RangeIndicator to current range
    public void RangeChange()
    {
        //try to initialize if it isn't set
        if (!sphereCol)
        {
            Start();
            return;
        }

        //set collider radius to new level radius
        sphereCol.radius = options[curLvl].radius;
        //get current size and scale
        Vector3 currentSize = rangeIndicator.renderer.bounds.size;
        Vector3 scale = rangeIndicator.transform.localScale;
        //modify scale to desired range
        scale.z = options[curLvl].radius * 2 * scale.z / currentSize.z;
        scale.x = options[curLvl].radius * 2 * scale.x / currentSize.x;
        //set scale
        rangeIndicator.transform.localScale = scale;
    }


    //swaps the tower model to the one defined in the corresponding upgrade option
    public void TowerChange()
    {
        //instantiate new tower model at the same position and reparent it to the Tower Manager object
        GameObject newTower = (GameObject)Instantiate(options[curLvl].swapModel, transform.position, transform.rotation);
        newTower.transform.parent = GameObject.Find("Tower Manager").transform;
        //find the TowerBase component, we have to do some adjustments to it
        TowerBase newTowerBase = newTower.GetComponentInChildren<TowerBase>();
        //get the gameobject which holds the TowerBase script,
        //and rename it to keep the old name
        GameObject subTower = newTowerBase.gameObject;
        newTower.name = newTowerBase.gameObject.name = transform.name;

        //attach and store an Upgrade script to the actual tower object,
        //copy values from the old tower
        Upgrade newUpgrade = subTower.AddComponent<Upgrade>();
        newUpgrade.curLvl = this.curLvl;
        newUpgrade.options = this.options;
        newTowerBase.upgrade = newUpgrade;

        //apply passive powerups to new tower
        PowerUpManager.ApplyToSingleTower(newTowerBase, newUpgrade);

        //switch TowerBase component to active state - attacks enemies
        newTowerBase.enabled = true;

        //abort shot automatism of the new tower,
        //in order to take the last shot time of the old tower into account
        newTowerBase.CancelInvoke("CheckRange");
        float lastShot = towerScript.lastShot;
        float invokeInSec = options[curLvl].shootDelay + lastShot - Time.time;
        if (invokeInSec < 0) invokeInSec = 0f;
        newTowerBase.StartInvoke(invokeInSec);

        //find GUI component and assign new upgrade script,
        //we do this in order to update the upgrade gui tooltip for the new tower
        GUILogic gui = GameObject.Find("GUI").GetComponent<GUILogic>();
        gui.upgrade = newUpgrade;

        //signalize enemies that the old tower was despawned,
        //here we loop through all enemies in range and remove the old tower from them.
        //they detect the new tower automatically, since enemies are colliding with it.
        for (int i = 0; i < towerScript.inRange.Count; i++)
        {
            PoolManager.Props[towerScript.inRange[i].name].RemoveTower(towerScript);
        }

        //re-rotate turret to match the old turret, if there existed one,
        //else rotate turret to match the grid rotation
        if (newTowerBase.turret)
        {
            if (towerScript.turret)
                newTowerBase.turret.rotation = towerScript.turret.rotation;
            else
            {
                //define ray with down direction to get the grid beneath
                Ray ray = new Ray(transform.position + new Vector3(0, 0.5f, 0), -transform.up);
                RaycastHit hit;

                //raycast downwards of the tower against our grid mask to get the grid
                if (Physics.Raycast(ray, out hit, 20, SV.gridMask))
                {
                    Transform grid = hit.transform;
                    newTowerBase.turret.rotation = grid.rotation;
                }
            }
            //don't let the tower/turret rotation affect shot position rotation
            newTowerBase.shotPos.localRotation = Quaternion.identity;

            //enable turret rotation mechanism
            newTowerBase.turret.GetComponent<TowerRotation>().enabled = true;
        }

        //destroy (this) old tower
        Destroy(transform.parent.gameObject);
    }
}

//resource type for conversion
//mainly used in editor scripts
public enum CostType
{
    intValue, //integer
    floatValue //floating-point number
}

//class for all possible upgrade options
[System.Serializable]
public class UpgOptions
{
    //price each upgrade should cost
    public float[] cost;
    //attackable radius
    public float radius = 5;
    //projectile damage to deal to enemies
    public float damage = 1;
    //delay between two shots/projectiles
    public float shootDelay = 3;
    //possible enemy target count
    public int targetCount = 1;
    //change tower model on upgrade (optional)
    public GameObject swapModel;
}