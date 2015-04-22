/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//helper class to display a laser-like line renderer
public class LaserHelper : MonoBehaviour
{
    //projectile reference for target positions etc
    public Projectile proj;
    //line renderer reference
    public LineRenderer line;


    //initialize instance
    IEnumerator OnSpawn()
    {
        //wait for projectile script to be executed
        yield return new WaitForEndOfFrame();

        //set starting and ending positions of line renderer component
        //starting position is the position when instantiated,
        //ending position is the updated projectile position
        line.SetPosition(0, proj.startPos);
        line.SetPosition(1, transform.position);
        //enable line renderer component after setting positions.
        //when doing it the other way round, small artifacts will be visible
        line.enabled = true;
    }


    void Update()
    {
        //update ending point to the target/enemy position
        if (proj.target)
            line.SetPosition(1, proj.target.position);
        else
            line.SetPosition(1, proj.endPos);
    }


    void OnDespawn()
    {
        //disable line renderer component again when despawned
        line.enabled = false;
    }

}

