/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;


//repositions the object that holds this script on the ground
//used for shadow projection
public class RepositionBlob : MonoBehaviour
{
    //this transform
    private Transform trans;
    //parent transform
    private Transform parent;


    void Start()
    {
        //cache transforms
        trans = transform;
        parent = transform.parent;
    }


    void LateUpdate()
    {
        //constrain this object to always look downwards (flat, from top view)
        trans.rotation = Quaternion.LookRotation(Vector3.right);

        //check for ground and re-position the object (shadow) there
        //(don't scale it though, otherwise it will break drawcall batching)
        RaycastHit hit;
        if (Physics.Raycast(parent.position, Vector3.down, out hit, SV.worldMask))
        {
            trans.position = hit.point + new Vector3(0, 0.05f, 0);
        }
    }
}
