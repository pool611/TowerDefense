/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


//GridEditor.cs is utilized to create and position grids
[CustomEditor(typeof(GridManager))]
public class GridEditor : Editor
{
    private GridManager script;     //manager reference
    //cast ray only downwards against layer WorldLimit (13) to determine height of the ground
    //we cannot use SV.worldMask because it isn't set in the editor, only at runtime.
    private int mask = (1 << LayerMask.NameToLayer("WorldLimit"));

    //display inspector properties
    public override void OnInspectorGUI()
    {
        //show default variables of script "GridManager.cs"
        DrawDefaultInspector();
        //get reference to manager script
        script = (GridManager)target;  

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Grid"))
        {
            //number for naming new grids
            int nr = 0;
            //if we already have some generated grids, count up
            if (script.transform.childCount > 0)   
            {
                //get last grid and split name, e.g. "grid 1"
                string[] gridNr = script.transform.GetChild(script.transform.childCount - 1).name.Split(' ');
                //get second part of the name, the number,
                //increase it by one and assign that as our new grid no.
                nr = int.Parse(gridNr[1]) + 1;
            }

            //for height defined in inspector
            for (int i = 0; i < script.height; i++)
            {
                //and width defined there too
                for (int j = 0; j < script.width; j++)
                {
                    //instantiate grid
                    GameObject gridGO = (GameObject)PrefabUtility.InstantiatePrefab(script.gridPrefab);
                    Undo.RegisterCreatedObjectUndo(gridGO, "GridGeneration");
                        
                    //at manager gameobject position
                    gridGO.transform.position =     new Vector3(script.transform.position.x + script.gridSize / 2, script.transform.position.y,
                                                    script.transform.position.z - script.gridSize / 2) -
                                                    //right next to each other, with (or without) some offset
                                                    new Vector3(j * -script.gridSize - j * script.offsetX, 0, i * script.gridSize + i * script.offsetY);
                    //scale to desired size
                    gridGO.transform.localScale = new Vector3(script.gridSize, 0.01f, script.gridSize);
                    //parent to manager transform
                    gridGO.transform.parent = script.transform;
                    //name grid with calculated no. above
                    gridGO.name = ("grid " + nr);
                    nr++;   //increase no. by one
                }
            }

            //re-sort children by name
            foreach (Transform child in script.transform)
            {
                child.parent = child.parent;
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Place to Ground"))
        {
            //for each grid
            foreach (Transform trans in script.transform)
            {
                Undo.RecordObject(trans, "PlaceGround");

                //define ray to cast downwards
                Ray ray = new Ray(trans.position, -script.transform.up);
                RaycastHit hit;     //hit info holder
                //cast ray with a big length against ground
                if (Physics.Raycast(ray, out hit, 100, this.mask))
                {
                    //we cast against all WorldLimit objects (terrain, water, stones, ...)
                    //set grid y position to the point it hit the ground
                    trans.position = new Vector3(trans.position.x, hit.point.y, trans.position.z);
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();

        //whether generation process should check height of each grid -
        //if calculated height is greater than gridHeight (grid is too high in the air)
        //it will be added to taken grids, and the material will be changed to gridFullMat
        //(we cannot place a tower on this grid). On the other side it will be freed.
        if (GUILayout.Button("Check Height"))
        {
            //print a message if grid generator should check against a false number
            if (script.gridHeight <= 0)
            {
                Debug.Log("gridHeight has no value to check");
                return;
            }

            Undo.RecordObject(script, "CheckHeight");

            //generator checks grid height for every grid
            foreach (Transform trans in script.transform)
            {
                Undo.RecordObject(trans.renderer, "CheckHeight");

                //define ray, cast it downwards from grid position
                Ray ray = new Ray(trans.position + new Vector3(0, 0.1f, 0), -trans.up);
                RaycastHit hit;     //hit info holder
                //cast ray against ground with length equal to gridHeight
                //if it does NOT hit the ground, this grid is not available (too high in the air)
                if (!Physics.Raycast(ray, out hit, script.gridHeight, this.mask))
                {
                    //switch material to occupied material
                    trans.renderer.material = script.gridFullMat;
                    //add grid to "not available"-list if not already included
                    if (!script.GridList.Contains(trans.name))
                    script.GridList.Add(trans.name);
                }
                else
                {
                    //ray HIT the ground, it is available for tower placement,
                    //change material to gridFreeMat and remove from "not-available"-list
                    trans.renderer.material = script.gridFreeMat;
                    if(script.GridList.Contains(trans.name))
                        script.GridList.Remove(trans.name);
                }
            }
        }

        if (GUILayout.Button("Clear Height"))
        {
            //register scene undo step before we clear grid heights so we can revert to this state
            Undo.RecordObject(script, "ClearHeight");

            //since taken grids get added to "GridList" of GridManager.cs,
            //here we provide a simple button to reset those list
            script.GridList.Clear();

             //switch all grid materials to free material
            foreach (Transform trans in script.transform)
            {
                Undo.RecordObject(trans.renderer, "ClearHeight");
                trans.renderer.material = script.gridFreeMat;
            }
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        if (GUILayout.Button("Delete all Grids"))
        {
            //register scene undo step before we delete all grids so we can revert to this state
            Undo.RecordObject(script, "DeleteGrids");

            //display a short dialog to make sure we want to perform this action
            if (EditorUtility.DisplayDialog("Delete all Grids?",
                "Are you sure you want to delete all grids placed\nin your scene?", "Delete", "Cancel"))
            {
                //free "grid-taken"-list
                script.GridList.Clear();

                //as long as our Grid Manager has grids as child, execute destroy
                while (script.transform.childCount > 0)
                {
                    foreach (Transform trans in script.transform)
                    {
                        //destroy grid in editor mode
                        Undo.DestroyObjectImmediate(trans.gameObject);
                    }
                }
            }
        }


        //abort if nothing set
        if (script.gridHeight <= 0) return;

        //for each grid
        foreach (Transform trans in script.transform)
        {
            //define ray to cast downwards
            //we set the ray position beneath our object
            Ray ray = new Ray(trans.position + new Vector3(0, 0.1f, 0), -trans.up);
            RaycastHit hit;     //hit info holder

            //cast ray with a length equal to gridHeight against ground
            if (Physics.Raycast(ray, out hit, script.gridHeight, this.mask))
            {
                //we cast against all objects (terrain, water, stones, ...)
                Debug.DrawLine(ray.origin, hit.point, Color.yellow);
            }
            else
            {
                //the ray has not hit anything, draw red line downwards
                Debug.DrawRay(ray.origin, script.gridHeight * -trans.up, Color.red);
            }
        }
    }
}
