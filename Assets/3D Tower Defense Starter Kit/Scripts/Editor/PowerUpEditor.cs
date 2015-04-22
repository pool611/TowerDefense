/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

//custom powerup editor window
public class PowerUpEditor : EditorWindow
{
    [SerializeField]
    PowerUpManager powerUpScript;  //manager reference
    //track if the amount of powerups has been changed while drawing the gui
    private bool guiChange = false;
    //current window
    private static PowerUpEditor powerUpEditor;

    //toolbars for switching between powerup types and passive powerup editors
    private int toolbar = 0;
    private int passiveToolbar = 0;
    private string[] toolbarStrings = new string[] { "Battle Power Ups", "Passive Power Ups" };
    private string[] passiveToolbarStrings = new string[] { "Settings", "Requirements" };

    //inspector scrollbar x/y position, modified by mouse input
    //the battle/passive powerup inspector is divided into two parts, thus two scroll positions
    private Vector2 scrollPosBattleSelector;
    private Vector2 scrollPosBattleEditor;
    private Vector2 scrollPosPassiveSelector;
    private Vector2 scrollPosPassiveEditor;
    private Vector2 scrollPosPassiveReqEditor;

    //color to display for each battle powerup type
    private Color offensiveColor = new Color(1, 0, 0, 0.4f);
    private Color defensiveColor = new Color(0, 0, 1, 0.4f);

    //serialized objects to display,
    //access to the manager script and the selected powerup
    private SerializedObject script;
    private SerializedProperty battlePowerUpToEdit;
    private SerializedProperty passivePowerUpToEdit;
    //selected powerup as instances
    private BattlePowerUp selectedBattle;
    private PassivePowerUp selectedPassive;
    //currently dragging passive powerup requirement
    private int currentlyDragged = -1;
    //available filter of towers and enemies for passive powerups
    private string[] selectableTowers = new string[0];
    private string[] selectableEnemies = new string[0];


    //add menu named "PowerUp Settings" to the window menu
    [MenuItem("Window/TD Starter Kit/PowerUp Settings")]
    static void Init()
    {
        //get existing open window or if none, make a new one:
        powerUpEditor = (PowerUpEditor)EditorWindow.GetWindowWithRect(typeof(PowerUpEditor), new Rect(0, 0, 800, 400), false, "PowerUp Settings");
        //automatically repaint whenever the scene has changed (for caution)
        powerUpEditor.autoRepaintOnSceneChange = true;
    }
   

    //when the window gets opened
    void OnEnable()
    {
        //get reference to PowerUp Manager gameobject if we open the PowerUp Settings
        GameObject pumGO = GameObject.Find("PowerUp Manager");

        //could not get a reference, gameobject not created? debug warning.
        if (pumGO == null)
        {
            Debug.LogError("Current Scene contains no PowerUp Manager.");
            powerUpEditor.Close();
            return;
        }

        //get reference to PowerUp Manager script and cache it
        powerUpScript = pumGO.GetComponent<PowerUpManager>();

        //could not get component, not attached? debug warning.
        if (powerUpScript == null)
        {
            Debug.LogWarning("No PowerUp Manager Component found!");
            powerUpEditor.Close();
            return;
        }

        //set access to PowerUp Manager as serialized reference
        script = new SerializedObject(powerUpScript);
        Undo.undoRedoPerformed += OnUndoRedoPerformed;

        //set available filters
        FindTowers();
        FindEnemies();
    }


    //fill selectable towers array with towers in the TowerManager
    void FindTowers()
    {
        TowerManager towScript = null;
        GameObject towGO = GameObject.Find("Tower Manager");
        if (towGO)
            towScript = towGO.GetComponent<TowerManager>();
        if (towScript)
            selectableTowers = towScript.towerNames.ToArray();
    }


    //fill selectable enemies with enemies defined in the PoolManager
    void FindEnemies()
    {
        GameObject pmGO = GameObject.Find("Pool Manager");
        if (pmGO == null) return;

        GameObject poolGO = pmGO.transform.FindChild("Enemies").gameObject;
        if (poolGO == null) return;

        Pool enemyPool = poolGO.GetComponent<Pool>();
        if (enemyPool == null) return;

        List<string> enemies = new List<string>();
        for (int i = 0; i < enemyPool._PoolOptions.Count; i++)
        {
            if (enemyPool._PoolOptions[i].prefab == null)
                continue;

            enemies.Add(enemyPool._PoolOptions[i].prefab.name);
        }
        selectableEnemies = enemies.ToArray();
    }


    void OnGUI()
    {
        //we loose the reference on restarting unity and letting the Wave Editor open,
        //or by starting and stopping the runtime, here we make sure to get it again 
        if (powerUpScript == null)
            OnEnable();

        //display toolbar at the top, followed by a horizontal line
        toolbar = GUILayout.Toolbar(toolbar, toolbarStrings);
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
        
        //handle toolbar selection
        switch (toolbar)
        {
            //first tab selected
            //draw the battle powerup selector on the left side,
            //and the battle powerup editor on the right side
            case 0:
                EditorGUILayout.BeginHorizontal();
                DrawBattlePowerUpsSelector();
                DrawBattlePowerUpsEditor();
                EditorGUILayout.EndHorizontal();
                break;
            //second tab selected
            //draw passive powerup window
            case 1:
                EditorGUILayout.BeginHorizontal();
                DrawPassivePowerUpsSelector();
                EditorGUILayout.BeginVertical();
                passiveToolbar = GUILayout.Toolbar(passiveToolbar, passiveToolbarStrings);
                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                //another tab for editing properties or requirements
                switch (passiveToolbar)
                {
                    case 0:
                        DrawPassivePowerUpsEditor();
                        break;
                    case 1:
                        DrawPassivePowerUpsReqEditor();
                        break;
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                break;
        }

        //track if the gui has changed by user input
        TrackChange(guiChange);
    }


    //draws a list for all battle powerups
    void DrawBattlePowerUpsSelector()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(270));

        //begin a scrolling view inside GUI, pass in current Vector2 scroll position 
        scrollPosBattleSelector = EditorGUILayout.BeginScrollView(scrollPosBattleSelector, true, true, GUILayout.Height(325), GUILayout.Width(270));

        //iterate over all battle powerups in the main list
        for (int i = 0; i < powerUpScript.battlePowerUps.Count; i++)
        {
            //get the current powerup
            BattlePowerUp powerup = powerUpScript.battlePowerUps[i];

            //differentiate between offensive and defensive powerup,
            //set the gui color correspondingly
            if (powerup is OffensivePowerUp)
                GUI.backgroundColor = offensiveColor;
            else if (powerup is DefensivePowerUp)
                GUI.backgroundColor = defensiveColor;

            //draw a box with the color defined above
            //and reset the color to white
            GUI.Box(new Rect(5, i * 28, 25, 25), i + " ");
            GUI.backgroundColor = Color.white;

            //compare powerup in the list with the currently selected one
            //if it's the selected one, tint the background yellow
            if (powerup == selectedBattle)
                GUI.backgroundColor = Color.yellow;
            GUI.Box(new Rect(25, i * 28, 225, 25), "");
            GUI.backgroundColor = Color.white;
        }

        //draw the list of offensive powerups,
        //then draw the list of defensive powerups below
        if (Event.current.type != EventType.ValidateCommand)
        {
            DrawBattlePowerUps(script.FindProperty("battleOffensive"), powerUpScript.battleOffensive);
            DrawBattlePowerUps(script.FindProperty("battleDefensive"), powerUpScript.battleDefensive);
        }

        //ends the scrollview defined above
        EditorGUILayout.EndScrollView();

        //start button layout at the bottom of the left side
        //draw box with the background color defined at the beginning
        //begin with the offensive powerup add button
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = offensiveColor;
        GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(15));
        GUI.backgroundColor = Color.white;

        //add a new offensive powerup to the list
        if (GUILayout.Button("Add Offensive Power Up"))
        {
            Undo.RecordObject(powerUpScript, "AddOffensive");
            Undo.RecordObject(this, "AddOffensive");

            //create new instance
            OffensivePowerUp newOff = new OffensivePowerUp();
            //insert new powerup at the end of the offensive list
            //also add the new powerup to the main list of battle powerups
            powerUpScript.battlePowerUps.Insert(powerUpScript.battleOffensive.Count, newOff);
            powerUpScript.battleOffensive.Add(newOff);
            //mark that the gui changed and update the script values
            guiChange = true;
            script.Update();
            //select the newly created powerup,
            //also select the powerup as active selection for editing
            selectedBattle = newOff;
            battlePowerUpToEdit = script.FindProperty("battleOffensive").GetArrayElementAtIndex(powerUpScript.battleOffensive.Count - 1);
        }

        EditorGUILayout.EndHorizontal();
        //continue with the offensive powerup add button
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = defensiveColor;
        GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(15));
        GUI.backgroundColor = Color.white;

        //add a new defensive powerup to the list
        if (GUILayout.Button("Add Defensive Power Up"))
        {
            Undo.RecordObject(powerUpScript, "AddDefensive");
            Undo.RecordObject(this, "AddDefensive");

            //create new instance
            DefensivePowerUp newDef = new DefensivePowerUp();
            //add new powerup to the end of the defensive list
            //also add the new powerup to the main list of battle powerups
            powerUpScript.battlePowerUps.Add(newDef);
            powerUpScript.battleDefensive.Add(newDef);
            //mark that the gui changed and update the scipt values
            guiChange = true;
            script.Update();
            //select the newly created powerup as active selection and for editing
            selectedBattle = newDef;
            battlePowerUpToEdit = script.FindProperty("battleDefensive").GetArrayElementAtIndex(powerUpScript.battleDefensive.Count - 1);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }


    //draws the "Edit" and "Delete" button for each powerup in the list
    void DrawBattlePowerUps<T>(SerializedProperty list, List<T> repList) where T : BattlePowerUp
    {
        //iterate over the serialized powerup list
        //used for both offensive and defensive powerups
        for (int i = 0; i < list.arraySize; i++)
        {
            if (repList == null || repList.Count == 0
                || list.arraySize != repList.Count) return;

            //get current actual powerup instance
            BattlePowerUp powerup = repList[i];

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(30);
            //draw the powerup name
            GUILayout.Label(powerup.name, GUILayout.Width(145));
            
            //draw edit button that selects this powerup
            if (GUILayout.Button("Edit", GUILayout.Width(35)))
            {
                //select powerup as editable reference
                selectedBattle = powerup;
                battlePowerUpToEdit = list.GetArrayElementAtIndex(i);
                //deselect other gui fields,
                //otherwise user input is carried over
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
            }

            //draw delete button for this powerup
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                Undo.RecordObject(powerUpScript, "DeletePowerup");
                Undo.RecordObject(this, "DeletePowerup");

                //unset editable selection
                selectedBattle = null;
                battlePowerUpToEdit = null;
                //remove powerup in the main list
                //and actual list for the type
                powerUpScript.battlePowerUps.RemoveAt(i);
                repList.RemoveAt(i);
                //mark that the gui changed and update the scipt values
                guiChange = true;
                script.Update();
                Repaint();
                return;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
    }


    //draws the battle powerup editor
    void DrawBattlePowerUpsEditor()
    {
        //get offensive and defensive powerups and
        //insert them in the main list on every gui update
        //this makes sure that we always display current values
        for (int i = 0; i < powerUpScript.battleOffensive.Count; i++)
            powerUpScript.battlePowerUps[i] = powerUpScript.battleOffensive[i];
        for (int i = 0; i < powerUpScript.battleDefensive.Count; i++)
            powerUpScript.battlePowerUps[i + powerUpScript.battleOffensive.Count] = powerUpScript.battleDefensive[i];
        //update the scipt values
        script.Update();

        //do not draw the following editor window
        //if no editable selection was set
        if (battlePowerUpToEdit == null || !powerUpScript.battlePowerUps.Contains(selectedBattle))
        {
            GUILayout.BeginArea(new Rect((Screen.width / 2) + 50, (Screen.height / 2), 150, 100));
            GUILayout.Label("No Power Up selected.", EditorStyles.boldLabel);
            GUILayout.EndArea();
            return;
        }

        //begin a scrolling view inside GUI, pass in current Vector2 scroll position 
        scrollPosBattleEditor = EditorGUILayout.BeginScrollView(scrollPosBattleEditor);
        //draw custom inspector field for the powerup, including children
        EditorGUILayout.PropertyField(battlePowerUpToEdit, true);
        //ends the scrollview defined above
        EditorGUILayout.EndScrollView();
        //push modified values back to the manager script
        script.ApplyModifiedProperties();
    }


    //draws a list for all battle powerups
    void DrawPassivePowerUpsSelector()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(270));

        //begin a scrolling view inside GUI, pass in current Vector2 scroll position 
        scrollPosPassiveSelector = EditorGUILayout.BeginScrollView(scrollPosPassiveSelector, false, true, GUILayout.Height(325), GUILayout.Width(270));
        GUI.backgroundColor = Color.white;

        //iterate over all passive powerups
        for (int i = 0; i < powerUpScript.passivePowerUps.Count; i++)
        {
            GUI.Box(new Rect(5, i * 28, 25, 25), i + " ");

            //compare powerup in the list with the currently selected one
            //if it's the selected one, tint the background yellow
            if (powerUpScript.passivePowerUps[i] == selectedPassive)
                GUI.backgroundColor = Color.yellow;

            Rect box = new Rect(25, i * 28, 225, 25);
            GUI.Box(box, "");
            GUI.backgroundColor = Color.white;

            box.width = 150;
            //re-use background box with smaller width for determining currently dragged index
            if (Event.current.type == EventType.MouseDown && box.Contains(Event.current.mousePosition))
                currentlyDragged = i;
            else if (Event.current.type == EventType.MouseUp)
                currentlyDragged = -1;
            //repaint window after mouse events
            this.Repaint();
        }

        //iterate over the powerup list
        for (int i = 0; i < powerUpScript.passivePowerUps.Count; i++)
        {
            //get the current powerup
            PassivePowerUp powerup = powerUpScript.passivePowerUps[i];

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(30);
            //draw the powerup name
            GUILayout.Label(powerup.name, GUILayout.Width(145));

            //draw edit button that selects this powerup
            if (GUILayout.Button("Edit", GUILayout.Width(35)))
            {
                //select powerup as editable reference
                selectedPassive = powerup;
                SerializedProperty prop = script.FindProperty("passivePowerUps");
                if (prop.arraySize > i)
                    passivePowerUpToEdit = prop.GetArrayElementAtIndex(i);
                else
                {
                    script.Update();
                    return;
                }
                //deselect other gui fields,
                //otherwise user input is carried over
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
            }

            //draw delete button for this powerup
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                Undo.RecordObject(powerUpScript, "DeletePowerup");
                Undo.RecordObject(this, "DeletePowerup");

                //unset editable selection
                selectedPassive = null;
                passivePowerUpToEdit = null;
                //remove powerup in the main list
                //remove references to other requirements
                RemovePassiveReq(i);
                powerUpScript.passivePowerUps.RemoveAt(i);
                //mark that the gui changed and update the scipt values
                guiChange = true;
                script.Update();
                return;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
        
        //ends the scrollview defined above
        EditorGUILayout.EndScrollView();
        //add a new offensive powerup to the list
        if (GUILayout.Button("Add Passive Power Up", GUILayout.Height(40)))
        {
            //UNITY CRASHES IF THESE ARE ADDED
            //Undo.RecordObject(powerUpScript, "AddPassive");
            //Undo.RecordObject(this, "AddPassive");

            //create new instance
            PassivePowerUp newPass = new PassivePowerUp();
            //insert new powerup at the end of the list
            powerUpScript.passivePowerUps.Add(newPass);
            //mark that the gui changed and update the script values
            guiChange = true;
            script.Update();
            //select the newly created powerup,
            //also select the powerup as active selection for editing
            selectedPassive = newPass;
            passivePowerUpToEdit = script.FindProperty("passivePowerUps").GetArrayElementAtIndex(powerUpScript.passivePowerUps.Count - 1);
        }

        EditorGUILayout.EndVertical();
    }


    //draws the passive power up editor window
    void DrawPassivePowerUpsEditor()
    {
        //do not draw the following editor window
        //if no editable selection was set
        if (passivePowerUpToEdit == null)
        {
            GUILayout.BeginArea(new Rect((Screen.width / 2) + 50, (Screen.height / 2), 150, 100));
            GUILayout.Label("No Power Up selected.", EditorStyles.boldLabel);
            GUILayout.EndArea();
            return;
        }

        //fetch serialized values
        script.Update();
        //begin a scrolling view inside GUI, pass in current Vector2 scroll position 
        scrollPosPassiveEditor = EditorGUILayout.BeginScrollView(scrollPosPassiveEditor);

        //draw property fields for all base attributes of PassivePowerUp
        EditorGUILayout.PropertyField(passivePowerUpToEdit.FindPropertyRelative("name"));
        EditorGUILayout.PropertyField(passivePowerUpToEdit.FindPropertyRelative("description"));
        EditorGUILayout.PropertyField(passivePowerUpToEdit.FindPropertyRelative("icon"));
        EditorGUILayout.PropertyField(passivePowerUpToEdit.FindPropertyRelative("defaultSprite"));
        EditorGUILayout.PropertyField(passivePowerUpToEdit.FindPropertyRelative("enabledSprite"));
        EditorGUILayout.PropertyField(passivePowerUpToEdit.FindPropertyRelative("onEnabledIcon"));
        EditorGUILayout.PropertyField(passivePowerUpToEdit.FindPropertyRelative("locked"));
        EditorGUILayout.PropertyField(passivePowerUpToEdit.FindPropertyRelative("enabled"));
        EditorGUILayout.PropertyField(passivePowerUpToEdit.FindPropertyRelative("cost"), true);
        EditorGUILayout.Separator();

        //draw affected towers selection popups
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Affected Towers:", GUILayout.Width(145));
        //index for current selection in the popup
        int selectIndex = 0;
        //construct list of selections and add special entries,
        //then add the list of towers we found earlier
        List<string> selection = new List<string>();
        selection.Add("Add Towers");
        selection.Add("ALL");
        selection.AddRange(selectableTowers);
        string[] selectable = selection.ToArray();

        //display tower popup selection
        selectIndex = EditorGUILayout.Popup(selectIndex, selectable);
        //"ALL" was selected, add all towers to the powerup
        if (selectIndex == 1)
        {
            for (int i = 2; i < selectable.Length; i++)
            {
                if (!selectedPassive.selectedTowers.Contains(selectable[i]))
                    selectedPassive.selectedTowers.Add(selectable[i]);
            }
        }
        //otherwise just try to add the selected tower to the powerup
        else if (selectIndex > 1 && !selectedPassive.selectedTowers.Contains(selectable[selectIndex]))
            selectedPassive.selectedTowers.Add(selectable[selectIndex]);

        //clear selection and rearrange list with selected towers,
        //for the possibility of removing them again
        selection.Clear();
        selection.Add("Remove Towers");
        selection.AddRange(selectedPassive.selectedTowers);
        selectable = selection.ToArray();

        //set selection back to zero, display tower popup selection
        GUILayout.Label("=>", GUILayout.Width(25));
        selectIndex = 0;
        selectIndex = EditorGUILayout.Popup(selectIndex, selectable);
        if (selectIndex > 0)
            selectedPassive.selectedTowers.RemoveAt(selectIndex - 1);
        EditorGUILayout.EndHorizontal();

        //do the same with enemies, draw affected enemy popup
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Affected Enemies:", GUILayout.Width(145));
        selectIndex = 0;
        selection.Clear();
        selection.Add("Add Enemies");
        selection.Add("ALL");
        selection.AddRange(selectableEnemies);
        selectable = selection.ToArray();

        //display enemy popup selection
        selectIndex = EditorGUILayout.Popup(selectIndex, selectable);
        if (selectIndex == 1)
        {
            for (int i = 2; i < selectable.Length; i++)
            {
                if (!selectedPassive.selectedEnemies.Contains(selectable[i]))
                    selectedPassive.selectedEnemies.Add(selectable[i]);
            }
        }
        else if (selectIndex > 1 && !selectedPassive.selectedEnemies.Contains(selectable[selectIndex]))
            selectedPassive.selectedEnemies.Add(selectable[selectIndex]);

        //prepare enemy selection removal popup
        selection.Clear();
        selection.Add("Remove Enemies");
        selection.AddRange(selectedPassive.selectedEnemies);
        selectable = selection.ToArray();

        //set selection back to zero, display enemy popup selection
        GUILayout.Label("=>", GUILayout.Width(25));
        selectIndex = 0;
        selectIndex = EditorGUILayout.Popup(selectIndex, selectable);
        if (selectIndex > 0)
            selectedPassive.selectedEnemies.RemoveAt(selectIndex - 1);
        EditorGUILayout.EndHorizontal();

        //draw inspector fields for all powerup properties, including children
        EditorGUILayout.PropertyField(passivePowerUpToEdit.FindPropertyRelative("towerProperties"), true);
        EditorGUILayout.PropertyField(passivePowerUpToEdit.FindPropertyRelative("enemyProperties"), true);
        EditorGUILayout.PropertyField(passivePowerUpToEdit.FindPropertyRelative("playerProperties"), true);

        //ends the scrollview defined above
        EditorGUILayout.EndScrollView();

        //push modified values back to the manager script
        script.ApplyModifiedProperties();
    }


    //draws the passive power up requirement window
    void DrawPassivePowerUpsReqEditor()
    {
        //do not draw the following editor window
        //if no editable selection was set
        if (passivePowerUpToEdit == null)
        {
            GUILayout.BeginArea(new Rect((Screen.width / 2) + 50, (Screen.height / 2), 150, 100));
            GUILayout.Label("No Power Up selected.", EditorStyles.boldLabel);
            GUILayout.EndArea();
            return;
        }
        //fetch serialized values
        script.Update();
        //yellow lines, symbolizing dependencies
        GUI.color = Color.yellow;
        GUI.Box(new Rect(527, 238, 5, 40), "");
        GUI.Box(new Rect(327, 238, 5, 100), "");
        GUI.Box(new Rect(728, 238, 5, 100), "");
        GUI.Box(new Rect(327, 334, 149, 5), "");
        GUI.Box(new Rect(584, 334, 149, 5), "");
        GUI.color = Color.white;

        //display sprite boxes surrounding the sprite
        Rect myPos = new Rect(490, 290, 80, 80);
        GUI.Box(new Rect(myPos.x - 5, myPos.y - 5, myPos.width + 10, myPos.height + 25), "");
        GUI.Box(myPos, "");

        //draw sprite texture with calculated uv offset,
        //along with the powerup name
        if (selectedPassive.icon && selectedPassive.icon.sprite)
        {
            GUI.color = selectedPassive.icon.color;
            GUI.DrawTexture(myPos, selectedPassive.icon.mainTexture);
            GUI.color = Color.white;
        }
        GUI.Label(new Rect(myPos.x, myPos.y + myPos.height, myPos.width, 20), selectedPassive.name);

        //begin a scrolling view inside GUI, pass in current Vector2 scroll position 
        scrollPosPassiveReqEditor = EditorGUILayout.BeginScrollView(scrollPosPassiveReqEditor, GUILayout.Width(530), GUILayout.Height(175));

        //invisible label for dynamically resizing the window width based on requirement count
        int windowWidth = 10 + selectedPassive.req.Count * 100;
        GUILayout.Label("", GUILayout.Width(windowWidth), GUILayout.Height(0));

        //tint the following controls in yellow if we have a dragged item
        if (currentlyDragged >= 0) GUI.color = Color.yellow;
        //draw window for dropping the currently dragged powerup into and cache dimensions
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(150));
        Rect dropBox = GUILayoutUtility.GetLastRect();
        GUI.color = Color.white;

        //we released our dragged item
        if (Event.current.type == EventType.MouseUp && currentlyDragged >= 0)
        {
            //if the mouse was inside the drop box,
            //add the dragged powerup as a new requirement to the current one
            if (dropBox.Contains(Event.current.mousePosition)
                && selectedPassive != powerUpScript.passivePowerUps[currentlyDragged]
                && !selectedPassive.req.Contains(currentlyDragged))
            {
                selectedPassive.req.Add(currentlyDragged);
                guiChange = true;
                this.Repaint();
            }
            else
                Debug.Log("Can't add Requirement. The selected Powerup already contains it.");
            //reset drag index
            currentlyDragged = -1;
        }

        //if the powerup does not contain a requirement yet, show a info label
        if (selectedPassive.req.Count == 0)
        {
            GUI.Label(new Rect(160 + dropBox.x, 60 + dropBox.y, 200, 20), "Drag & Drop Powerups HERE", EditorStyles.boldLabel);
            EditorGUILayout.EndScrollView();
            return;
        }

        //loop through requirements and draw them on screen,
        //just as we did with the powerup icon sprite
        for (int i = 0; i < selectedPassive.req.Count; i++)
        {
            PassivePowerUp req = powerUpScript.passivePowerUps[selectedPassive.req[i]];

            Rect pos = new Rect(20 + i * 100, 20, 80, 80);
            GUI.Box(new Rect(pos.x - 5, pos.y - 5, pos.width + 10, pos.height + 50), "");
            GUI.Box(pos, "");
            if (req.icon && req.icon.sprite)
            {
                GUI.color = req.icon.color;
                GUI.DrawTexture(pos, req.icon.mainTexture);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(pos.x, 20 + pos.height, pos.width, 20), req.name);
            
            //additionally show a button for removing the requirement
            if (GUI.Button(new Rect(pos.x, 40 + pos.height, pos.width, 20), "X"))
            {
                selectedPassive.req.RemoveAt(i);
                return;
            }
        }
        //ends the scrollview defined above
        EditorGUILayout.EndScrollView();

        //push modified values back to the manager script
        script.ApplyModifiedProperties();
    }


    //helper method to remove a passive powerup and cleaning up requirement references
    void RemovePassiveReq(int index)
    {
        //loop through powerups
        for (int i = 0; i < powerUpScript.passivePowerUps.Count; i++)
        {
            PassivePowerUp powerup = powerUpScript.passivePowerUps[i];
            //backward loop over requirements
            for (int j = powerup.req.Count - 1; j >= 0; j--)
            {
                //this powerup has the old powerup as a requirement,
                //remove it
                if(powerup.req[j] == index)
                {
                    powerup.req.RemoveAt(j);
                    continue;
                }
                //lower all requirements above the removed powerup index
                if (powerup.req[j] > index)
                    powerup.req[j] -= 1;
            }
        }
    }


    void OnUndoRedoPerformed()
    {
        Debug.Log("Undo Redo called");

        battlePowerUpToEdit = null;
        passivePowerUpToEdit = null;
        selectedBattle = null;
        selectedPassive = null;
    }


    void TrackChange(bool change)
    {
        //if we typed in other values in the editor window,
        //we need to repaint it in order to display the new values
        if (GUI.changed || change)
        {
            //we have to tell Unity that a value of the PowerUp Manager script has changed
            //http://unity3d.com/support/documentation/ScriptReference/EditorUtility.SetDirty.html
            EditorUtility.SetDirty(powerUpScript);
            //repaint editor GUI window
            Repaint();
        }
    }
}