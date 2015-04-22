/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;

//UVscroll.cs sets an offet to our material texture, so it seems like it is animating.
//(used on aimRenderer Prefab to visualize aim direction)
public class UVscroll : MonoBehaviour
{
    //define scrolling speed
    public float scrollSpeed = 0.5f;


    void LateUpdate()
    {
        //calculate texture offset based on time
        float offset = Time.time * scrollSpeed;
        //set offset to material main texture in (x,y) axis
        renderer.material.mainTextureOffset = new Vector2(-offset, 0);
    } 
}