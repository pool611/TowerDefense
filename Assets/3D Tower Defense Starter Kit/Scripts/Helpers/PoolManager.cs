/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//this class stores all Pool references and provides additional Pool functionality
//we access pools through the 'Pools' dictionary, but there are also methods
//to add or remove pools as well as destroying unused instances of a pool
public class PoolManager : MonoBehaviour 
{

    //Pool.cs references
    public static Dictionary<string, Pool> Pools = new Dictionary<string, Pool>();
    //Properties.cs script references per enemy, Projectile.cs accesses these cached components
    //so no further GetComponent() calls are needed every time a projectile hits an enemy.
    //that doesn't really have something to do with object pooling, but it fits here
    public static Dictionary<string, Properties> Props = new Dictionary<string, Properties>();


    //static variables always keep their values over scene changes
    //so we need to reset them when the game ended (not at game start with Awake(),
    //because Pool.cs initializes itself with Awake() and we would remove it again immediately)
    void OnDestroy()
    {
        Pools.Clear();
        Props.Clear();
    }


    //add new pool to dictionary - called by Pool.cs on Awake()
    //for creating a new pool at runtime use CreatePool()
	public static void Add(Pool pool) 
    {
        //debug error if pool was already added before 
        if (Pools.ContainsKey(pool.name))
        {
            Debug.LogError("Pool Manager already contains Pool: " + pool.name);
            return;
        }

        //add name and script reference to dictionary
        Pools.Add(pool.name, pool);
	}


    //create new pool at runtime
    public static void CreatePool(string poolName, GameObject prefab, int preLoad, bool limit, int maxCount)
    {
        //debug error if pool was already added before 
        if (Pools.ContainsKey(poolName))
        {
            Debug.LogError("Pool Manager already contains Pool: " + poolName);
            return;
        }

        //create new gameobject which will hold the new pool component
        GameObject newPoolGO = new GameObject(poolName);
        //add pool component to the new object
        Pool newPool = newPoolGO.AddComponent<Pool>();

        //initialize a new pool option for this pool
        PoolOptions newPoolOptions = new PoolOptions();
        //assign default parameters passed in
        newPoolOptions.prefab = prefab;
        newPoolOptions.preLoad = preLoad;
        newPoolOptions.limit = limit;
        newPoolOptions.maxCount = maxCount;
        
        //add new pool option to its list
        newPool._PoolOptions.Add(newPoolOptions);
        //directly instantiate new instances
        newPool.PreLoad();
    }


    //simple check if the dictionary contains the given pool
    //returns true or false
    public static bool ContainsPool(string poolName)
    {
        if (Pools.ContainsKey(poolName))
            return true;
        else
            return false;
    }


    //simple check if the dictionary contains the given pool
    //returns the pool reference
    public static Pool GetPool(string poolName)
    {
        //debug error if pool does not exist
        if (!Pools.ContainsKey(poolName))
        {
            Debug.LogError("Pool Manager couldn't find Pool to return: " + poolName);
            return null;
        }

        return Pools[poolName];
    }


    //destroy specific pool by name
    public static void DeactivateAllInstances(string poolName)
    {
        //debug error if pool wasn't already added before
        if (!Pools.ContainsKey(poolName))
        {
            Debug.LogError("Pool Manager couldn't find Pool to deactive instances: " + poolName);
            return;
        }

        //cache list of pool options
        List<PoolOptions> tempPool = Pools[poolName]._PoolOptions;
        //loop through each pool option
        for (int i = 0; i < tempPool.Count; i++)
        {
            //loop through each active instance and deactivate it
            for (int j = 0; j < tempPool[i].active.Count; j++)
            {
                PoolManager.Pools[poolName].Despawn(tempPool[i].active[j]);
            }
        }
    }


    //destroy specific pool by name
    public static void DestroyPool(string poolName)
    {
        //debug error if pool wasn't already added before
        if (!Pools.ContainsKey(poolName))
        {
            Debug.LogError("Pool Manager couldn't find Pool to destroy: " + poolName);
            return;
        }

        //destroy pool gameobject including all children - active/inactive instances
        Destroy(Pools[poolName].gameObject);
        //remove key-value pair from dictionary
        Pools.Remove(poolName);
    }


    //destroy all pools of the dictionary
    public static void DestroyAllPools()
    {
        //loop through the dictionary and destroy every pool gameobject
        //including children - active/inactive instances
        foreach (KeyValuePair<string, Pool> keyValuePair in Pools)
            Destroy(keyValuePair.Value.gameObject);

        //empty pool references
        Pools.Clear();
        Props.Clear();
    }


    //destroy all inactive instances of all pools
    //the parameter 'limitToPreLoad' decides if only instances
    //above the preload value of its pool should be destroyed
    public static void DestroyAllInactive(bool limitToPreLoad)
    {
        //loop through the dictionary and call DestroyUnused() on every pool
        foreach (KeyValuePair<string, Pool> keyValuePair in Pools)
            keyValuePair.Value.DestroyUnused(limitToPreLoad);
    }
}
