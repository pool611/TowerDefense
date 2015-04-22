/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//WaypointManager.cs grants access to all paths and its waypoints
public class WaypointManager : MonoBehaviour
{

    //this dictionary stores all path names and for each path its manager component with waypoint positions
    //enemies will receive their specific path component
    public static readonly Dictionary<string, PathManager> Paths = new Dictionary<string, PathManager>();


    //execute this before any other Start() or Update() function
    //since we need the data of all paths before we call them
    void Awake()
    {
        //reset static dictionary at game start (do not keep paths between games)
        Paths.Clear();
        //for each child/path of this gameobject, add path to dictionary
        foreach (Transform path in transform)
        {
            AddPath(path.gameObject);
        }
    }


    //this adds a path to the dictionary above, so our walker objects can access them
    public static void AddPath(GameObject path)
    {
        //check if path contains the name "Clone" (path was instantiated)
        if (path.name.Contains("Clone"))
        {
            //replace/remove "(Clone)" with an empty character
            path.name = path.name.Replace("(Clone)", "");
        }

        //check if path dictionary already contains this path name
        if (Paths.ContainsKey(path.name))
        {
            //debug warning and abort
            Debug.LogWarning("Called AddPath() but Scene already contains Path " + path.name + ".");
            return;
        }

        //get PathManager component
        PathManager pathMan = path.GetComponent<PathManager>();

        //if pathMan is null, so our path GameObject has no PathManager, debug warning and abort
        if (pathMan == null)
        {
            Debug.LogWarning("Called AddPath() but Transform " + path.name + " has no PathManager attached.");
            return;
        }

        //add path name and its manager reference to above dictionary to allow indirect access
        Paths.Add(path.name, pathMan);
    }

}

