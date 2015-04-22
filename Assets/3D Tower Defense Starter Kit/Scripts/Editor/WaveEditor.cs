/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

//custom wave editor window
public class WaveEditor : EditorWindow
{
    [SerializeField]
    WaveManager waveScript;  //manager reference
    //inspector scrollbar x/y position, modified by mouse input
    Vector2 scrollPos;
    //current window
    private static WaveEditor waveEditor;


    // Add menu named "Wave Editor" to the Window menu
    [MenuItem("Window/TD Starter Kit/Wave Settings")]
    static void Init()
    {
        //get existing open window or if none, make a new one:
        waveEditor = (WaveEditor)EditorWindow.GetWindowWithRect(typeof(WaveEditor), new Rect(0,0,800,400), false, "Wave Settings");
        //automatically repaint whenever the scene has changed (for caution)
        waveEditor.autoRepaintOnSceneChange = true;
    }


    void OnGUI()
    {
        //we loose the reference on restarting unity and letting the Wave Editor open,
        //or by starting and stopping the runtime, here we make sure to get it again 
        //get reference to Wave Manager gameobject if we open the Wave Settings
        GameObject wavesGO = GameObject.Find("Wave Manager");

        //could not get a reference, gameobject not created? debug warning.
        if (wavesGO == null)
        {
            Debug.LogError("Current Scene contains no Wave Manager.");
            waveEditor.Close();
            return;
        }

        //get reference to Wave Manager script and cache it
        waveScript = wavesGO.GetComponent<WaveManager>();

        //could not get component, not attached? debug warning.
        if (waveScript == null)
        {
            Debug.LogWarning("No Wave Manager Component found!");
            waveEditor.Close();
            return;
        }
        
        //track if the amount of waves has been changed while drawing the gui
        bool waveChange = false;

        EditorGUILayout.BeginHorizontal();

        //button to add a new wave
        if (GUILayout.Button("+++ Add Wave +++"))
        {
            Undo.RecordObject(waveScript, "AddWave");

            //create new wave option
            WaveOptions newWave = new WaveOptions();
            //initialize first row / creep of the new wave
            newWave.enemyPrefab.Add(null);
            newWave.enemyHP.Add(0);
            newWave.enemySH.Add(0);
            newWave.enemyCount.Add(1);
            newWave.startDelayMin.Add(0);
            newWave.startDelayMax.Add(0);
            newWave.delayBetweenMin.Add(0);
            newWave.delayBetweenMax.Add(0);
            newWave.path.Add(null);
            //add new wave option to wave list
            waveScript.options.Add(newWave);
            //set wave manipulation variable to true
            waveChange = true;
        }

        //button to delete all waves
        if (GUILayout.Button("Delete all Waves"))
        {
            Undo.RecordObject(waveScript, "DeleteWaves");
            waveScript.options.Clear();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        //begin a scrolling view inside GUI, pass in current Vector2 scroll position 
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(370));

        //editor window layout, top row with all labels
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("No. |", EditorStyles.boldLabel, GUILayout.Width(40));
        GUILayout.Label("      Prefab      ", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.Label("| Overwrite | Overwrite", EditorStyles.boldLabel, GUILayout.Width(155));
        GUILayout.Label("|  Count  |", EditorStyles.boldLabel, GUILayout.Width(70));
        GUILayout.Label(" Spawn-Delay  |", EditorStyles.boldLabel, GUILayout.Width(110));
        GUILayout.Label("Delay-Between |", EditorStyles.boldLabel, GUILayout.Width(115));
        GUILayout.Label("      Path      ", EditorStyles.boldLabel, GUILayout.Width(90));
        GUILayout.Label("| DELETE", EditorStyles.boldLabel, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        //additional description
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(170);
        GUILayout.Label("Health", EditorStyles.boldLabel, GUILayout.Width(75));
        GUILayout.Label("Shield", EditorStyles.boldLabel, GUILayout.Width(80));
        GUILayout.Space(70);
        GUILayout.Label("From-To", EditorStyles.boldLabel, GUILayout.Width(110));
        GUILayout.Label("From-To", EditorStyles.boldLabel, GUILayout.Width(125));
        EditorGUILayout.EndHorizontal();


        //if no wave was defined / wave list is empty, do not continue
        if (waveScript.options.Count == 0)
        {
            EditorGUILayout.EndScrollView();
            return;
        } 

        EditorGUILayout.Space();

        //for each wave, display wave properties and delete button
        for (int i = 0; i < waveScript.options.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            //show wave no.
            GUILayout.Label((i+1) + "", GUILayout.Width(90));
            GUILayout.Space(580);

            //show remove wave button ( this will remove all enemies contained in this wave )
            if (GUILayout.Button("X"))
            {
                Undo.RecordObject(waveScript, "DeleteWave");
                //remove the whole wave and skip further code for this iteration
                waveScript.options.RemoveAt(i);
                //abort further execution but do track undo
                TrackChange(true);
                //draw editor gui again
                return;
            }
            
            EditorGUILayout.EndHorizontal();

            //for each enemy within each wave, display an input row
            for (int j = 0; j < waveScript.options[i].enemyPrefab.Count; j++)
            {
                //if final delay of an enemy would be greater than the seconds between waves,
                //this would result in shifted enemies (e.g. an enemy defined in wave 1 would spawn in wave 2)
                //here we check if the delay of an enemy exceeds the delay of a wave and debug a warning
                //we only need to do this if seconds between waves are used at all - only in 'interval'
                //allowedDelay also observes incrementing seconds between waves
                if (waveScript.waveStartOption == WaveManager.WaveStartOption.interval)
                {
                    float finalDelay = waveScript.options[i].startDelayMax[j] + 
                                       (waveScript.options[i].enemyCount[j] - 1) * waveScript.options[i].delayBetweenMax[j];
                    float allowedDelay = waveScript.secBetweenWaves + i * waveScript.secIncrement;

                    //enemy delay exceeds specified wave delay and an enemy prefab is set,
                    //debug warning
                    if (finalDelay > allowedDelay && waveScript.options[i].enemyPrefab[j] != null)
                    {
                        Debug.LogWarning("Delay of Enemy " + waveScript.options[i].enemyPrefab[j].name + ", Wave " +
                                         (i + 1) + " exceeds Delay between Waves");
                    }
                }

                //Pre 1.4 compatibility checks for adding the new health and shield value rows
                for (int h = waveScript.options[i].enemyHP.Count; h < waveScript.options[i].enemyPrefab.Count; h++)
                    waveScript.options[i].enemyHP.Add(0);
                for (int h = waveScript.options[i].enemySH.Count; h < waveScript.options[i].enemyPrefab.Count; h++)
                    waveScript.options[i].enemySH.Add(0);

                EditorGUILayout.BeginHorizontal();
                
                //display enemy row with all properties
                GUILayout.Space(40);
                //show enemy prefab slot
                waveScript.options[i].enemyPrefab[j] = (GameObject)EditorGUILayout.ObjectField(waveScript.options[i].enemyPrefab[j], typeof(GameObject), false, GUILayout.Width(115));
                GUILayout.Space(15);
                //overwrite hp and/or shield values
                waveScript.options[i].enemyHP[j] = EditorGUILayout.FloatField(waveScript.options[i].enemyHP[j], GUILayout.Width(50));
                GUILayout.Space(20);
                waveScript.options[i].enemySH[j] = EditorGUILayout.FloatField(waveScript.options[i].enemySH[j], GUILayout.Width(50));
                GUILayout.Space(25);
                //amount of this enemy
                waveScript.options[i].enemyCount[j] = EditorGUILayout.IntField(waveScript.options[i].enemyCount[j], GUILayout.Width(40));
                GUILayout.Space(35);
                //show start delay input field
                waveScript.options[i].startDelayMin[j] = EditorGUILayout.FloatField(waveScript.options[i].startDelayMin[j], GUILayout.Width(25));
                waveScript.options[i].startDelayMax[j] = EditorGUILayout.FloatField(waveScript.options[i].startDelayMax[j], GUILayout.Width(25));
                GUILayout.Space(55);
                //show delay between each enemy input field
                waveScript.options[i].delayBetweenMin[j] = EditorGUILayout.FloatField(waveScript.options[i].delayBetweenMin[j], GUILayout.Width(25));
                waveScript.options[i].delayBetweenMax[j] = EditorGUILayout.FloatField(waveScript.options[i].delayBetweenMax[j], GUILayout.Width(25));
                GUILayout.Space(35);
                //show path to follow - slot
                waveScript.options[i].path[j] = (PathManager)EditorGUILayout.ObjectField(waveScript.options[i].path[j], typeof(PathManager), true, GUILayout.Width(105));

                //remove enemy row button
                if (GUILayout.Button("X"))
                {
                    Undo.RecordObject(waveScript, "DeleteRow");
                    //we remove all properties of this enemy in the specific wave
                    waveScript.options[i].enemyPrefab.RemoveAt(j);
                    waveScript.options[i].enemyHP.RemoveAt(j);
                    waveScript.options[i].enemySH.RemoveAt(j);
                    waveScript.options[i].enemyCount.RemoveAt(j);
                    waveScript.options[i].startDelayMin.RemoveAt(j);
                    waveScript.options[i].startDelayMax.RemoveAt(j);
                    waveScript.options[i].delayBetweenMin.RemoveAt(j);
                    waveScript.options[i].delayBetweenMax.RemoveAt(j);
                    waveScript.options[i].path.RemoveAt(j);

                    //if this was the last enemy of this wave, we remove the wave too
                    if (waveScript.options[i].enemyPrefab.Count == 0)
                        waveScript.options.RemoveAt(i);

                    //abort further execution but do track undo
                    TrackChange(true);
                    //draw editor gui again
                    return;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.Space();

            //add new enemy to this wave button
            if (GUILayout.Button("Add Enemy"))
            {
                Undo.RecordObject(waveScript, "AddRow");
                //initialize new row / creep properties and add them to this wave
                waveScript.options[i].enemyPrefab.Add(null);
                waveScript.options[i].enemyHP.Add(0);
                waveScript.options[i].enemySH.Add(0);
                waveScript.options[i].enemyCount.Add(1);
                waveScript.options[i].startDelayMin.Add(0);
                waveScript.options[i].startDelayMax.Add(0);
                waveScript.options[i].delayBetweenMin.Add(0);
                waveScript.options[i].delayBetweenMax.Add(0);
                waveScript.options[i].path.Add(null);
                //set wave manipulation variable to true
                waveChange = true;
            }

            //button to insert a new wave below the current one
            if (i < (waveScript.options.Count-1) && GUILayout.Button("Insert Wave"))
            {
                Undo.RecordObject(waveScript, "AddWave");
                //create new wave option
                WaveOptions newWave = new WaveOptions();
                //initialize first row / creep of the new wave
                newWave.enemyPrefab.Add(null);
                newWave.enemyHP.Add(0);
                newWave.enemySH.Add(0);
                newWave.enemyCount.Add(1);
                newWave.startDelayMin.Add(0);
                newWave.startDelayMax.Add(0);
                newWave.delayBetweenMin.Add(0);
                newWave.delayBetweenMax.Add(0);
                newWave.path.Add(null);
                //insert new wave option to wave list
                //waveScript.options.Add(newWave);
                waveScript.options.Insert(i+1, newWave);
                //set wave manipulation variable to true
                waveChange = true;
            }

            EditorGUILayout.Space();

            EditorGUILayout.EndHorizontal();
        }

        //ends the scrollview defined above
        EditorGUILayout.EndScrollView();
        //track if the gui has changed by user input
        TrackChange(waveChange);
    }


    void TrackChange(bool waveChange)
    {
        //if we typed in other values in the editor window,
        //we need to repaint it in order to display the new values
        if (GUI.changed || waveChange)
        {
            //we have to tell Unity that a value of the WaveManager script has changed
            //http://unity3d.com/support/documentation/ScriptReference/EditorUtility.SetDirty.html
            EditorUtility.SetDirty(waveScript);
            //repaint editor GUI window
            Repaint();
        }
    }
}