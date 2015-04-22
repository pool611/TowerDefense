/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEditor;

//userfriendly EditorWindow to setup new projectile prefabs out of models
public class ProjectileSetup : EditorWindow
{
	//projectile model slot within the window and prefab after instantiation
    public GameObject projectileModel;

	//collider type attached to the prefab
    public enum ColliderType
    {
        boxCollider,
        sphereCollider,
        capsuleCollider,
    }
    //default ColliderType value
    public ColliderType colliderType = ColliderType.sphereCollider;

	//collider bounds of projectile model
    Bounds totalBounds = new Bounds();
    //renderer components for calculating model bounds
    private Renderer[] renderers;

    //projectile layer number, default layer value is 'Projectiles'
    public int layer = LayerMask.NameToLayer("Projectiles");
    //attach Projectile component to this prefab?
    public bool attachProjectile = true;
    //attach Rigidbody component to this prefab?
    public bool attachRigidbody = true;

    
    // Add menu named "Projectile Setup" to the Window menu
    [MenuItem("Window/TD Starter Kit/Projectile Setup")]
    static void Init()
    {
        //get existing open window or if none, make a new one:
        EditorWindow.GetWindow(typeof(ProjectileSetup));
    }

	
	//draw custom editor window GUI
    void OnGUI()
    {
        //display label and object field for projectile model slot 
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Projectile Model:");
        projectileModel = (GameObject)EditorGUILayout.ObjectField(projectileModel, typeof(GameObject), false);
        EditorGUILayout.EndHorizontal();

		//display label and enum list for collider type
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Collider Type:");
        colliderType = (ColliderType)EditorGUILayout.EnumPopup(colliderType);
        EditorGUILayout.EndHorizontal();

		//display label and layer field for projectile layer
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Projectile Layer:");
        layer = EditorGUILayout.LayerField(layer);
        EditorGUILayout.EndHorizontal();

        //display label and checkbox for Projectile component
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Attach Projectile:");
        attachProjectile = EditorGUILayout.Toggle(attachProjectile);
        EditorGUILayout.EndHorizontal();

        //display label and checkbox for Rigidbody component
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Attach Rigidbody:");
        attachRigidbody = EditorGUILayout.Toggle(attachRigidbody);
        EditorGUILayout.EndHorizontal();

		//display info box below all settings
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("By clicking on 'Apply!' all chosen components are added and a prefab will be created next to your projectile model.", MessageType.Info);
        EditorGUILayout.Space();

		//apply button
        if (GUILayout.Button("Apply!"))
        {
            //cancel further execution if no tower model is set
            if (projectileModel == null)
            {
                Debug.LogWarning("No projectile model chosen. Aborting Projectile Setup execution.");
                return;
            }

			//get model's asset path in this project to place the new prefab next to it 
            string assetPath = AssetDatabase.GetAssetPath(projectileModel.GetInstanceID());
            //e.g. assetPath = "Assets/Models/model.fbx
            //split folder structure for renaming the existing model name as prefab
            string[] folders = assetPath.Split('/');
            //e.g. folders[0] = "Assets", folders[1] = "Models", folders[2] = "model.fbx"
            //then we replace the last part, the model name in folders[2], with the new prefab name
            assetPath = assetPath.Replace(folders[folders.Length - 1], projectileModel.name + ".prefab");
            //new asset path: "Assets/Models/model.prefab"


            //instantiate, convert and setup model to new prefab
            ProcessModel();

			//if Projectile checkbox is checked
            if (attachProjectile)
            {              
                //attach Projectile component
                projectileModel.AddComponent<Projectile>();
            }

			//if Rigidbody checkbox is checked, add component
            if (attachRigidbody)
            {
                //attach and store rigidbody component
                Rigidbody rigid = projectileModel.AddComponent<Rigidbody>();
                //make rigidbody kinematic
                rigid.isKinematic = true;
                //disable gravity
                rigid.useGravity = false;
            }

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
                    prefab = PrefabUtility.CreatePrefab(assetPath, projectileModel.gameObject);
                }
            }
            else
                //we haven't created a prefab before nor the project contains one,
            	//create prefab next to the model at assetPath
                prefab = PrefabUtility.CreatePrefab(assetPath, projectileModel.gameObject);

            //destroy temporary instantiated projectile model in the editor
            DestroyImmediate(projectileModel.gameObject);
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
        //temporary instantiate projectile model for creating a prefab of it later
        projectileModel = (GameObject)Instantiate(projectileModel);
        //rename instance name, remove "(Clone)"
        projectileModel.name = projectileModel.name.Replace("(Clone)", "");
        
		//get all renderers of this model instance to calculate object bounds
		//used to setup the collider
        renderers = projectileModel.GetComponentsInChildren<Renderer>();

		//if the model has no renderer / mesh, debug a warning and skip collider setup
        if (renderers.Length == 0)
            Debug.LogWarning("Projectile Model contains no Renderer! Skipping Collider.");
        else
        {
            //for each attached renderer of this projectile model
        	//adjust bounds variable to include all mesh bounds 
            foreach (Renderer renderer in renderers)
            {
                totalBounds.Encapsulate(renderer.bounds);
            }
			//add a collider with these bounds
            AddCollider();
        }
        
		//set model instance layer
        projectileModel.layer = layer;
    }


    void AddCollider()
    {
       	//attach a collider to the model instance depending on the ColliderType selection
        switch (colliderType)
        {
            //add box collider, reposition center relative to the model instance
        	//set size to calculated bounds
            case ColliderType.boxCollider:
                BoxCollider boxCol = projectileModel.AddComponent<BoxCollider>();
                boxCol.center = totalBounds.center - projectileModel.transform.position;
                boxCol.size = totalBounds.size;
                break;
                
            //add sphere collider, reposition center relative to the model instance
        	//set radius to calculated bounds width
            case ColliderType.sphereCollider:
                SphereCollider sphereCol = projectileModel.AddComponent<SphereCollider>();
                sphereCol.center = totalBounds.center - projectileModel.transform.position;
                sphereCol.radius = totalBounds.extents.y;
                break;
                
            //add capsule collider, reposition center relative to the model instance
        	//set radius to calculated bounds width, height to bounds height
            case ColliderType.capsuleCollider:
                CapsuleCollider capsuleCol = projectileModel.AddComponent<CapsuleCollider>();
                capsuleCol.center = totalBounds.center - projectileModel.transform.position;
                capsuleCol.radius = totalBounds.extents.x;
                capsuleCol.height = totalBounds.size.y;
                break;
        }
    }
}
