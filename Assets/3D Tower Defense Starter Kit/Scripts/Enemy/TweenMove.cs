/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Holoville.HOTween;
using Holoville.HOTween.Plugins;

//this script handles enemy movement, which is based on HOTween and our implemented waypoint system.
//Also it sends a path progress calculation to the ProgressMap
//and has methods to slow the enemy down
public class TweenMove : MonoBehaviour 
{
    //which path to call, assigned by Wave Manager
    [HideInInspector]
    public PathManager pathContainer;
    //animation path type, linear or curved, curved by default
    public PathType pathtype = PathType.Curved;
    //should this gameobject look to its target point?
    public bool orientToPath = false;
    //custom object size to add
    public float sizeToAdd = 0;
    //time or speed value
    public float speed = 5;
    //maximum walk speed
    //(at start and without slowdown this is equal to variable speed)
    [HideInInspector]
    public float maxSpeed;
    //cached tween timeScale for permanent changes
    [HideInInspector]
    public float tScale = 1f;
    //cache all waypoint position references of the requested path
    private Transform[] waypoints;

    //--HOTween animation helper variables--
    //active HOTween tween
    public Tweener tween = null;
    //array of modified waypoint positions for the tween
    private Vector3[] wpPos;
    //parameters for the tween
    private TweenParms tParms;
    //HOTween path plugin for curved movement
    private PlugVector3Path plugPath;

    //ProgressMap Integration
    public ProgMapProps pMapProperties = new ProgMapProps();


    //initialize waypoint positions and progress map access on every spawn
    IEnumerator OnSpawn()
    {
        //wait one frame so Start() gets called before OnSpawn() on a new instantiation
        yield return new WaitForEndOfFrame();

        //get Vector3 array with waypoint positions
        waypoints = pathContainer.waypoints;

        //get waypoint array of Vector3
        InitWaypoints();

        //start movement
        StartMove();

        //add this enemy to ProgressMap to track path progress
        if (pMapProperties.enabled)
        {
            //add to progress map - identify by gameobject ID
            //all further path progress calculation happens in ProgressCalc() and ProgressMap.cs
            ProgressMap.AddToMap(pMapProperties.prefab, pMapProperties.myID);
            //start calculating our path progress a few times per second
            //(more accurate and less overhead than calling it in an update function)
            InvokeRepeating("ProgressCalc", 0.5f, pMapProperties.updateInterval);
        }
    }


    //initialize waypoint positions
    internal void InitWaypoints()
    {
        //recreate array used for waypoint positions
        wpPos = new Vector3[waypoints.Length];
        //fill array with original positions and add custom height
        for (int i = 0; i < wpPos.Length; i++)
        {
            wpPos[i] = waypoints[i].position + new Vector3(0, sizeToAdd, 0);
        }
    }


    //starts movement
    public void StartMove()
    {
        //we set this gameobject position directly to the first waypoint and
        //we also add a defined size to our object height,
        //so our gameobject could "stand" on top of the path.
        transform.position = wpPos[0];
        //we're now at the first waypoint position,
        //so directly call the next waypoint
        CreateTween();
    }


    //creates a new HOTween tween which moves us to the next waypoint
    //(defined by passed arguments)
    internal void CreateTween()
    {
        //prepare HOTween's parameters, you can look them up here
        //http://www.holoville.com/hotween/documentation.html
        ////////////////////////////////////////////////////////////
        
        //create new HOTween plugin for curved paths
        //pass in array of Vector3 waypoint positions, relative = true
        plugPath = new PlugVector3Path(wpPos, true, pathtype);

        //orients the tween target along the path
        //constrains this game object on one axis
        if (orientToPath)
            plugPath.OrientToPath();

        //create TweenParms for storing HOTween's parameters
        tParms = new TweenParms();
        //sets the path plugin as tween position property
        tParms.Prop("position", plugPath);
        tParms.AutoKill(true);
        //use speed based tween with linear easing
        tParms.SpeedBased();
        tParms.Ease(EaseType.Linear);
        tParms.OnComplete(OnPathComplete);

        //create a new tween, move this gameobject with given arguments
        tween = HOTween.To(transform, maxSpeed, tParms);
    }


    //call on path completed
    internal void OnPathComplete()
    {
        //signal that our path has ended, receiver is Properties.cs, it will despawn this object
        gameObject.SendMessage("PathEnd", SendMessageOptions.DontRequireReceiver);
    }


    //Path Distance/Progress Calculation and ProgressMap access
    void ProgressCalc()
    {
        //calculate current progress which consists of the time we walked already,
        //and the total time of the tween
        float progress = tween.fullElapsed / tween.fullDuration;
	
        //let our ProgressMap display this object:
        //pass in the gameobject ID of this object to identify it and its total path progress
        ProgressMap.CalcInMap(pMapProperties.myID, progress);
    }


    //slow down based on current speed - maxSpeed variables
    public void Slow()
    {
        //calulate new timeScale value based on original speed
        float newValue = speed / maxSpeed;
        tween.timeScale = tScale * newValue;
    }


    //reset speed to maximum speed for disregarding any slow
    public void Accelerate()
    {
        //float multiplier = maxSpeed / speed;
        speed = maxSpeed;
        //reset timescale
        tween.timeScale = tScale;
    }


    //called on death
    //reset all initialized variables for later use
    void OnDespawn()
    {
        //reset speed in case this object was killed with slow applied
        speed = maxSpeed;
        tScale = 1f;
    }


    //ProgressMap Properties for this enemy
    [System.Serializable]
    public class ProgMapProps
    {
         //if this walker should use the progress map 
        public bool enabled = false;
        //icon prefab to show
        public GameObject prefab;
        //interval at which the progress should get updated
        public float updateInterval = 0.25f;

        //stores an unique ID of this gameobject at runtime
        //to identify it over its lifetime
        [HideInInspector]
        public int myID;
    }
}
