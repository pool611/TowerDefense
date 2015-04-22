/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;

//TowerRotation.cs rotates our tower turret
public class TowerRotation : MonoBehaviour
{
    //damping factor of our turret
    public float damping = 0f;
    //tower base script of parent, set by TowerBase on Start()
    [HideInInspector]
	public TowerBase towerScript;


    void Awake()
    {
        this.enabled = false;
    }


    void Update()
    {
        //store current target of TowerBase.cs
        Transform rotateTo = towerScript.currentTarget;

        //perform check if 'rotateTo' is a valid target -> it isn't walked out of range
        //or was destroyed between two CheckRange() calls of TowerBase.cs
        //enemies remove themselves from the 'inRange' list in these cases
        if (rotateTo && !towerScript.inRange.Contains(rotateTo.gameObject))
        {
            //target not valid - get new target
            //if there is another one in range, use the first one
            if (towerScript.inRange.Count > 0)
                rotateTo = towerScript.inRange[0].transform;
            else
                //'currentTarget' of TowerBase.cs is obsolete
                //and no other enemies are in range, reset 'rotateTo'
                rotateTo = null;
        }
        
        //if no valid enemy was found (e.g. out of range or destroyed) - abort and return
        if(!rotateTo)
            return;

        //get enemy direction to rotate to (current tower target)
        Vector3 dir = rotateTo.position - transform.position;
        //rotate with damping to enemy position
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime / (damping / 10));
        
		//restrict result only to y axis (do not rotate turret up- or downwards)
		transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, 0f);
    }
}