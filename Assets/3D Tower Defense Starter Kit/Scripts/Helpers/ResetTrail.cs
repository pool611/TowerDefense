/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;

//this script sets the width of the trail renderer to zero on deactivation,
//and resets it back to the original width/length when it gets reused.
//without this script, the recycling of trail renderers leaves artifacts due
//to fast position changing, when reused within one second after deactivation
//(unity thing?)
public class ResetTrail : MonoBehaviour
{
    //drag&drop trail renderer to reset
    public TrailRenderer trail;
    //variables to store start and end width
    private float startWidth;
    private float endWidth;

    //on first instantiation, get original width
    void Start()
    {
        startWidth = trail.startWidth;
        endWidth = trail.endWidth;
    }

    //reset trail to original width on reactivation
    void OnSpawn()
    {
        //do not execute further code if this object was freshly instantiated
        //and Start() wasn't called before, therefore startWidth equals zero
        if (startWidth == 0) return;

        trail.startWidth = startWidth;
        trail.endWidth = endWidth;
    }

    //zero out trail width on deactivation
    void OnDespawn()
    {
        trail.startWidth = 0f;
        trail.endWidth = 0f;
    }
}
