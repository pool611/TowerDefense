/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEditor;

//userfriendly EditorWindow to setup new tower prefabs out of models
public class TowerSetup : EditorWindow
{
	//tower model slot within the window and prefab after instantiation
    public GameObject towerModel;
    //range indicator prefab attached to the prefab
    public GameObject rangeIndicator;
    //range trigger prefab attached to the prefab
    public GameObject rangeTrigger;
	//collider type attached to the prefab
    public enum ColliderType
    {
        boxCollider,
        sphereCollider,
        capsuleCollider,
    }
    //default ColliderType value
    public ColliderType colliderType = ColliderType.capsuleCollider;

	//collider bounds of tower model
    Bounds totalBounds = new Bounds();
    //renderer components for calculating model bounds
    private Renderer[] renderers;

    //tower layer number, default layer value is 'Tower'
    public int layer = LayerMask.NameToLayer("Tower");
    //attach TowerBase component to this prefab?
    public bool attachTowerBase = true;
    //attach Upgrade component to this prefab?
    public bool attachUpgrade = true;

    
    // Add menu named "Tower Setup" to the Window menu
    [MenuItem("Window/TD Starter Kit/Tower Setup")]
    static void Init()
    {
        //get existing open window or if none, make a new one:
        EditorWindow.GetWindow(typeof(TowerSetup));
    }

	
	//draw custom editor window GUI
    void OnGUI()
    {
       	//display label and object field for tower model slot 
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Tower Model:");
        towerModel = (GameObject)EditorGUILayout.ObjectField(towerModel, typeof(GameObject), false);
        EditorGUILayout.EndHorizontal();

    	//display label and object field for range indicator prefab slot
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("RangeIndicator Prefab:");
        rangeIndicator = (GameObject)EditorGUILayout.ObjectField(rangeIndicator, typeof(GameObject), false);
        EditorGUILayout.EndHorizontal();

    	//display label and object field for range trigger prefab slot 
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("RangeTrigger Prefab:");
        rangeTrigger = (GameObject)EditorGUILayout.ObjectField(rangeTrigger, typeof(GameObject), false);
        EditorGUILayout.EndHorizontal();

		//display label and enum list for collider type
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Collider Type:");
        colliderType = (ColliderType)EditorGUILayout.EnumPopup(colliderType);
        EditorGUILayout.EndHorizontal();

		//display label and layer field for tower layer
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Tower Layer:");
        layer = EditorGUILayout.LayerField(layer);
        EditorGUILayout.EndHorizontal();

		//display label and checkbox for TowerBase component
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Attach TowerBase:");
        attachTowerBase = EditorGUILayout.Toggle(attachTowerBase);
        EditorGUILayout.EndHorizontal();

		//display label and checkbox for Upgrade component
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Attach Upgrade:");
        attachUpgrade = EditorGUILayout.Toggle(attachUpgrade);
        EditorGUILayout.EndHorizontal();

		//display info box below all settings
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("By clicking on 'Apply!' all chosen components are added and a prefab will be created next to your tower model.", MessageType.Info);
        EditorGUILayout.Space();

		//apply button
        if (GUILayout.Button("Apply!"))
        {
            //cancel further execution if no tower model is set
            if (towerModel == null)
            {
                Debug.LogWarning("No tower model chosen. Aborting Tower Setup execution.");
                return;
            }

			//get model's asset path in this project to place the new prefab next to it 
            string assetPath = AssetDatabase.GetAssetPath(towerModel.GetInstanceID());
            //e.g. assetPath = "Assets/Models/model.fbx
            //split folder structure for renaming the existing model name as prefab
            string[] folders = assetPath.Split('/');
            //e.g. folders[0] = "Assets", folders[1] = "Models", folders[2] = "model.fbx"
            //then we replace the last part, the model name in folders[2], with the new prefab name
            assetPath = assetPath.Replace(folders[folders.Length - 1], towerModel.name + ".prefab");
            //new asset path: "Assets/Models/model.prefab"

			//create new tower container gameobject
            Transform towerContainer = new GameObject(towerModel.name).transform;

            //instantiate, convert and setup model to new prefab
            ProcessModel();

			//parent tower instance to container gameobject and reposition it (relative Vector3.zero)
            towerModel.transform.parent = towerContainer;
            towerModel.transform.position = towerContainer.position;

			//if range indicator checkbox is checked
            if (rangeIndicator)
            {
            	//instantiate range indicator prefab
                rangeIndicator = (GameObject)Instantiate(rangeIndicator);
                //rename prefab clone to match naming conventions of Upgrade.cs
                //remove the "(Clone)" part of the name
                rangeIndicator.name = rangeIndicator.name.Replace("(Clone)", "");
                //parent range indicator to tower container
                rangeIndicator.transform.parent = towerContainer;
                //relatively reposition the range indicator slightly above to the tower container 
                rangeIndicator.transform.position = towerContainer.position + new Vector3(0, 0.2f, 0);
            }

			//if range trigger checkbox is checked
            if (rangeTrigger)
            {
            	//instantiate range trigger prefab
                rangeTrigger = (GameObject)Instantiate(rangeTrigger);
                //rename prefab clone to match naming conventions of Upgrade.cs
                //remove the "(Clone)" part of the name
                rangeTrigger.name = rangeTrigger.name.Replace("(Clone)", "");
                //parent range trigger to tower container
                rangeTrigger.transform.parent = towerContainer;
                //relatively reposition the range trigger in the middle of the tower container
                rangeTrigger.transform.position = towerContainer.position + new Vector3(0, totalBounds.extents.y, 0);
            }

			//if TowerBase checkbox is checked
            if (attachTowerBase)
            {
            	//create new gameobject used as shoot position and call it 'shotPos'
                GameObject shotPosObj = new GameObject("shotPos");
                //parent shotPosObj directly to the tower instance
                shotPosObj.transform.parent = towerModel.transform;
                //reposition shotPosObj
                shotPosObj.transform.position = towerModel.transform.position;
                //set shotPos object's layer to built-in layer 2 : ignore raycast
                shotPosObj.layer = 2;
                
                //attach and store TowerBase component
                TowerBase towerBase = towerModel.AddComponent<TowerBase>();
                //set shotPos transform to this component
                towerBase.shotPos = shotPosObj.transform;
                //disable the script, it only gets activated at runtime
                towerBase.enabled = false;

				//if we instantiated a range indicator, set it to this component
                if (rangeIndicator)
                    towerBase.rangeInd = rangeIndicator;
            }

			//if Upgrade checkbox is checked, add component
            if (attachUpgrade)
                towerModel.AddComponent<Upgrade>();

            //parent model to container
            towerModel.transform.parent = towerContainer;

			//initialize prefab gameobject
            GameObject prefab = null;

			//perform check if we already have a prefab in our project (null if none)
            if (AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)))
            {
                //display custom dialog and wait for user input to overwrite prefab
                if (EditorUtility.DisplayDialog("Are you sure?",
                "The prefab already exists. Do you want to overwrite it?",
                "Yes",
                "No"))
                {
                    //user clicked "Yes", create and overwrite existing prefab
                    prefab = PrefabUtility.CreatePrefab(assetPath, towerContainer.gameObject);
                }
            }
            else
                //we haven't created a prefab before nor the project contains one,
            	//create prefab next to the model at assetPath
                prefab = PrefabUtility.CreatePrefab(assetPath, towerContainer.gameObject);

			//destroy temporary instantiated tower model in the editor
            DestroyImmediate(towerContainer.gameObject);
            //if we created a prefab
            if (prefab)
            {
            	//select it within the project panel
                Selection.activeGameObject = prefab;
               	//close this editor window
                this.Close();
            }
        }
    }


    void ProcessModel()
    {
       	//temporary instantiate tower model for creating a prefab of it later
        towerModel = (GameObject)Instantiate(towerModel);
        //rename instance name, remove "(Clone)"
        towerModel.name = towerModel.name.Replace("(Clone)", "");
        //reposition at 0,0,0
        towerModel.transform.position = Vector3.zero;
        
		//get all renderers of this model instance to calculate object bounds
		//used to setup the collider
        renderers = towerModel.GetComponentsInChildren<Renderer>();

		//if the model has no renderer / mesh, debug a warning and skip collider setup
        if (renderers.Length == 0)
            Debug.LogWarning("Tower Model contains no Renderer! Skipping Collider.");
        else
        {
            //for each attached renderer of this tower model
        	//adjust bounds variable to include all mesh bounds 
            foreach (Renderer renderer in renderers)
            {
                totalBounds.Encapsulate(renderer.bounds);
            }
			//add a collider with these bounds
            AddCollider();
        }
        
		//set model instance layer
        towerModel.layer = layer;
    }


    void AddCollider()
    {
       	//attach a collider to the model instance depending on the ColliderType selection
        switch (colliderType)
        {
            //add box collider, reposition center relative to the model instance
        	//set size to calculated bounds
            case ColliderType.boxCollider:
                BoxCollider boxCol = towerModel.AddComponent<BoxCollider>();
                boxCol.center = totalBounds.center - towerModel.transform.position;
                boxCol.size = totalBounds.size;
                break;
                
            //add sphere collider, reposition center relative to the model instance
        	//set radius to calculated bounds width
            case ColliderType.sphereCollider:
                SphereCollider sphereCol = towerModel.AddComponent<SphereCollider>();
                sphereCol.center = totalBounds.center - towerModel.transform.position;
                sphereCol.radius = totalBounds.extents.y;
                break;
                
            //add capsule collider, reposition center relative to the model instance
        	//set radius to calculated bounds width, height to bounds height
            case ColliderType.capsuleCollider:
                CapsuleCollider capsuleCol = towerModel.AddComponent<CapsuleCollider>();
                capsuleCol.center = totalBounds.center - towerModel.transform.position;
                capsuleCol.radius = totalBounds.extents.x;
                capsuleCol.height = totalBounds.size.y;
                break;
        }
    }
}
