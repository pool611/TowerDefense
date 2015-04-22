/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;


//pool manager use case for example scene "Example_Pool"
public class UseCase : MonoBehaviour
{
    public GameObject prefabToSpawn1;
    public GameObject prefabToSpawn2;
    public GameObject prefabToSpawn3;

    public int instantiateCount;
    public float delay;


    //start coroutines
    void Start()
    {
        StartCoroutine("Spawn1");
        StartCoroutine("Spawn2");
        StartCoroutine("Spawn3");
    }


    //spawn Cube
    IEnumerator Spawn1()
    {
        for (int i = 0; i < 3; i++)
        {
            //spawn
            PoolManager.Pools["Boxes"].Spawn(prefabToSpawn1, new Vector3(-4, 0, i * 2), Quaternion.identity);
            
            yield return new WaitForSeconds(delay);
        }

        Debug.Log("Spawn finished");

        yield return new WaitForSeconds(1);
        
        for (int i = 0; i < 3; i++)
        {
            //get the first active instance of the pool "Boxes" with index 0 -> Cubes
            GameObject instance = PoolManager.Pools["Boxes"]._PoolOptions[0].active[0];
            //despawn
            PoolManager.Pools["Boxes"].Despawn(instance);
            
            yield return new WaitForSeconds(delay);
        }

        Debug.Log("Despawn finished");
    }

    
    //spawn crate
    IEnumerator Spawn2()
    {
        for (int i = 0; i < instantiateCount; i++)
        {
            //spawn
            PoolManager.Pools["Boxes"].Spawn(prefabToSpawn2, new Vector3(4, 0, i * 2), Quaternion.identity);
            
            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(1);

        for (int i = 0; i < instantiateCount; i++)
        {
            //get the first active instance of the pool "Boxes" with index 1 -> crates
            GameObject instance = PoolManager.Pools["Boxes"]._PoolOptions[1].active[0];
            //despawn
            PoolManager.Pools["Boxes"].Despawn(instance);
            
            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(1);
        //restart coroutine
        StartCoroutine("Spawn2");
    }


    //spawn ExplosionFX
    IEnumerator Spawn3()
    {
        for (int i = 0; i < instantiateCount; i++)
        {
            //spawn
            PoolManager.Pools["Particles"].Spawn(prefabToSpawn3, new Vector3(0, 0, i * 2), Quaternion.identity);

            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(1);
        //restart coroutine
        StartCoroutine("Spawn3");
    }   
}