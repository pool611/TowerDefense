/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//GridEditor.cs handles the inspector GUI for this class and the generation of grids
//GridManager.cs stores all necessary variables and the grid list (accessed via GUILogic.cs
//at the placement of towers)
public class GridManager : MonoBehaviour
{
    public GameObject gridPrefab;   //standard grid prefab to instantiate on grid generation
    public Material gridFreeMat;    //the material which indicates a grid is free
    public Material gridFullMat;    //the material which indicates a grid is occupied
    public int gridSize = 8;    //the scale of a grid
    public float offsetX = 0f;   //free space between grids on x axis
    public float offsetY = 0f;   //free space between grids on y axis

    public int width = 2;   //how many grids to instantiate on x axis
    public int height = 2;  //how many grids to instantiate on z axis

    public float gridHeight = 1f; //allowed grid height value, used by "Check Height" Button
    private bool gridVisible = true;    //grid visibility trigger
    //[HideInInspector]
    public List<string> GridList = new List<string>();  //a list of taken grids in our scene


    //disable visibility of all grids at start
    void Start()
    {
        ToggleVisibility(false);
    }


    public void ToggleVisibility(bool visible)
    {
        //only enable/disable renderer if input has changed
        if (gridVisible == visible) return;
        gridVisible = visible;

        //enable/disable renderer on all children
        //(if we would deactivate the whole grid gameobject instead,
        //we could not raycast against it anymore in order to
        //detect on which grid a tower is located)
        foreach (Transform trans in transform)
        {
            trans.renderer.enabled = gridVisible;
        }
    }
}
