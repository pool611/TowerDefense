/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Progress Map logic which visualizes the path progress for each object/enemy
public class ProgressMap : MonoBehaviour
{
    //we use this instance to start coroutines on this script,
    //because we can't start them directly from within a static method (see RemoveFromMap())
    public static ProgressMap instance;

    //store all object IDs and their necessary properties (script ProgressMapObject) here at runtime
    public static Dictionary<int, ProgressMapObject> objDic = new Dictionary<int, ProgressMapObject>();


    //reset and clear our progress map before the game starts
    void Awake()
    {
        //clear dictionaries with enemy IDs
        objDic.Clear();
        //set this script instance for coroutine access
        instance = this;
    }


    //add an object to the Progress Map, parameters are the prefab to be drawn on the map
    //and the gameobject ID for later access by a unique key. Called by TweenMove.cs in OnSpawn()
    public static void AddToMap(GameObject mapObjectPrefab, int objID)
    {
        //let PoolManager spawn the map icon prefab at the starting position of the map
        GameObject newObj = PoolManager.Pools["PM_StartingPoint"].Spawn(mapObjectPrefab,
                            Vector3.zero, Quaternion.identity);

        //get ProgressMapObject component of this map icon prefab
        ProgressMapObject newMapObj = newObj.GetComponent<ProgressMapObject>();

        //add this enemy with its ID and properties to the dictionary
        ProgressMap.objDic.Add(objID, newMapObj);
    }


    //let respective ProgressMapObject script set the current path progress slider of an object
    //called by TweenMove.cs per Invoke ProgressCalc() in OnSpawn()
    public static void CalcInMap(int objID, float curProgress)
    {
        objDic[objID].CalculateProgress(curProgress);
    }


    //object/enemy died or walked to the end of the path, we have to remove it from the map
    public static void RemoveFromMap(int objID)
    {
        //start coroutine below and pass in this object ID
        instance.StartCoroutine("Remove", objID);
    }


    internal IEnumerator Remove(int objID)
    {
        //cache ProgressMapObject script reference
        ProgressMapObject pMapObj = objDic[objID];

        //remove it from our dictionary
        objDic.Remove(objID);

        //change sprite to the one indicating this object/enemy died
        pMapObj.image.sprite = pMapObj.objDeadSprite;

        //wait 2 seconds (display the death icon for 2 seconds)
        yield return new WaitForSeconds(2);

        //remove this ui element from our scene
        PoolManager.Pools["PM_StartingPoint"].Despawn(pMapObj.gameObject);
    }
}
