/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;


public class ObjectCall : MonoBehaviour
{
    void OnSpawn()
    {
        Debug.Log(gameObject.name + ": I am spawned!");
    }


    void OnDespawn()
    {
        Debug.Log(gameObject.name + ": I am despawned!");
    }
}
