/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEditor;
using System.Collections;

//helper widget for setting upgrade values automatically
public class UpgradeHelper : EditorWindow
{
    //upgrade script to manipulate
    public Upgrade script;
    //inspector scrollbar x/y position, modified by mouse input
    Vector2 scrollPos;

    //upgradable level count
    public int levelSize = 1;
    //resource count
    public int costSize = 1;
    //resource type
    public CostType costType = CostType.intValue;

    //initial values (on first level)
    public float[] initCost;
    public float initRadius = 0f;
    public float initDamage = 0f;
    public float initDelay = 0f;
    public int initTargets = 1;
    
    //initial upgrade price (on second level)
    public float[] initUpgradeCost;
    
    //increasing values
    //(based on first and second level)
    public float[] incCost;
    public TDValue[] incCostType;
    public float incRadius;
    public TDValue incRadiusType;
    public float incDamage;
    public TDValue incDamageType;
    public float incDelay;
    public TDValue incDelayType;
    public float incTargets;
    public TDValue incTargetsType;


    // Add menu named "Upgrade Helper" to the Window menu
    [MenuItem("Window/TD Starter Kit/Upgrade Helper")]
    static void Init()
    {
        //get existing open window or if none, make a new one:
        EditorWindow.GetWindowWithRect(typeof(UpgradeHelper), new Rect(0,0, 300, 460));
    }

    void OnGUI()
    {
        //begin a scrolling view inside GUI, pass in current Vector2 scroll position 
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(460));

        //upgrade script object slot
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Upgrade Script:");
        script = (Upgrade)EditorGUILayout.ObjectField(script, typeof(Upgrade), true);
        EditorGUILayout.EndHorizontal();

        //level and resource count
        EditorGUILayout.Space();
        levelSize = EditorGUILayout.IntField("Level Size:", levelSize);
        costSize = EditorGUILayout.IntField("Resources:", costSize);
        costType = (CostType)EditorGUILayout.EnumPopup("Resource Type:", costType);

        //restrict level and resource count to one, if below
        if (levelSize <= 0) levelSize = 1;
        if (costSize <= 0) costSize = 1;

        //resize resource arrays to resource count
        if (initCost == null || initCost.Length != costSize)
        {
            initCost = new float[costSize];
            initUpgradeCost = new float[costSize];
            incCost = new float[costSize];
            incCostType = new TDValue[costSize];
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Initial Values:");

        //draw resource fields
        for (int i = 0; i < costSize; i++)
            initCost[i] = EditorGUILayout.FloatField("Cost [" + i + "]:", initCost[i]);

        //draw fields for the initial stats
        initRadius = EditorGUILayout.FloatField("Radius:", initRadius);
        initDamage = EditorGUILayout.FloatField("Damage:", initDamage);
        initDelay = EditorGUILayout.FloatField("Delay:", initDelay);
        initTargets = EditorGUILayout.IntField("Target Count:", initTargets);
        if (initTargets <= 0) initTargets = 1;

        //draw first upgrade price field
        EditorGUILayout.LabelField("First Upgrade Price:");
        for (int i = 0; i < costSize; i++)
            initUpgradeCost[i] = EditorGUILayout.FloatField("Cost [" + i + "]:", initUpgradeCost[i]);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Increase By Values:");

        //draw resource arrays for increasing values
        for (int i = 0; i < costSize; i++)
        {
            EditorGUILayout.BeginHorizontal();
            incCost[i] = EditorGUILayout.FloatField("Cost [" + i + "]:", incCost[i]);
            incCostType[i] = (TDValue)EditorGUILayout.EnumPopup(incCostType[i], GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }

        //fields for other base stats
        //with selectable enum: additive or percentual values
        EditorGUILayout.BeginHorizontal();
        incRadius = EditorGUILayout.FloatField("Radius:", incRadius);
        incRadiusType = (TDValue)EditorGUILayout.EnumPopup(incRadiusType, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        incDamage = EditorGUILayout.FloatField("Damage:", incDamage);
        incDamageType = (TDValue)EditorGUILayout.EnumPopup(incDamageType, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        incDelay = EditorGUILayout.FloatField("Delay:", incDelay);
        incDelayType = (TDValue)EditorGUILayout.EnumPopup(incDelayType, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        incTargets = EditorGUILayout.FloatField("Target Count:", incTargets);
        incTargetsType = (TDValue)EditorGUILayout.EnumPopup(incTargetsType, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        //info box
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("By clicking on 'Apply!' the chosen Upgrade script gets overwritten with the values defined above.", MessageType.Info);
        EditorGUILayout.Space();

        //start calculation on button press
        if (GUILayout.Button("Apply!"))
        {
            Overwrite();
        }

        //ends the scrollview defined above
        EditorGUILayout.EndScrollView();
    }


    //calculation method for auto-fill
    void Overwrite()
    {
        //abort without upgrade script
        if (script == null)
        {
            Debug.LogWarning("UpgradeHelper: Upgrade script has not been set. Aborting.");
            return;
        }

        //break prefab connection before applying new values,
        //because starting the game (play mode) would reset them on prefabs
        PrefabUtility.DisconnectPrefabInstance(script.gameObject);
        //remove existing upgrade levels and values
        script.options.Clear();

        //create initial option (first level)
        //and add it to the script
        UpgOptions initOpt = new UpgOptions();
        initOpt.cost = initCost;
        initOpt.radius = initRadius;
        initOpt.damage = initDamage;
        initOpt.shootDelay = initDelay;
        initOpt.targetCount = initTargets;
        script.options.Add(initOpt);

        //add remaining level options (empty, for now)
        for (int i = 1; i < levelSize; i++)
            script.options.Add(new UpgOptions());
        //set first upgrade price on the second level
        if (levelSize > 1)
            script.options[1].cost = initUpgradeCost;

        //fill increasing upgrade options for remaining levels
        for (int i = 1; i < levelSize; i++)
        {
            //get the current and last option
            UpgOptions opt = script.options[i];
            UpgOptions last = script.options[i-1];

            //calculate new values based on fixed or percentual value entered in the widget.
            //fixed values consider the current level number when calculating the current value.
            //percentual values consider the last level and multiply it with the value entered
            if (incRadiusType == TDValue.fix) opt.radius = initRadius + i * incRadius;
            else opt.radius = Mathf.Round(last.radius * (1f + incRadius) * 100f) / 100f;

            if (incDamageType == TDValue.fix) opt.damage = initDamage + i * incDamage;
            else opt.damage = Mathf.Round(last.damage * (1f + incDamage) * 100f) / 100f;

            if (incDelayType == TDValue.fix) opt.shootDelay = initDelay + i * incDelay;
            else opt.shootDelay = Mathf.Round(last.shootDelay * (1f + incDelay) * 100f) / 100f;

            if (incTargetsType == TDValue.fix) opt.targetCount = Mathf.RoundToInt(initTargets + i * incTargets);
            else opt.targetCount = Mathf.RoundToInt(last.targetCount * (1f + incTargets));

        }

        //fill resource cost values starting from the third level
        //(as they are based on the initial upgrade price on the second level)
        for (int i = 2; i < levelSize; i++)
        {
            //get the current and last option
            //and create temporary array
            UpgOptions opt = script.options[i];
            UpgOptions last = script.options[i - 1];
            float[] cost = new float[costSize];

            //loop over costs for each level
            for (int j = 0; j < costSize; j++)
            {
                float value;
                //with fixed costs, we multiply the current level number with the cost value
                //and add the initial cost. percentual values always increase the last price
                if (incCostType[j] == TDValue.fix) value = initUpgradeCost[j] + (i-1) * incCost[j];
                else value = last.cost[j] * (1f + incCost[j]);
                //round resource value calculated above based on selected cost type:
                //integer rounds to whole number, floating-point rounds to 2 decimals
                if (costType == CostType.intValue) value = (int)value;
                else value = Mathf.Round(value * 100f) / 100f;
                //assign the calculated value for this resource
                cost[j] = value;
            }
            //assign the calculated resources for this level
            opt.cost = cost;
        }
    }
}
