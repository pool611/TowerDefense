/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

//GUILogic.cs is the base for all GUI actions, such as tower buttons and tower purchase with grid selection,
//you can implement these methods in your own GUI script (e.g. see GUIImpl.cs)
public class GUILogic : MonoBehaviour
{
    //SCRIPT REFERENCES
    public TowerManager towerScript;        //access to tower prefab list
    public GridManager gridScript;          //access to grid list so we can check whether a grid is free or occupied
    public PowerUpManager powerUpScript;    //access to power ups for enabling them
    public Camera raycastCam;               //camera which casts a ray against grids and towers (usually the main camera)

    //RAYS & RAYCASTHITS
    private Ray ray;                //ray for grid and tower detection
    private RaycastHit gridHit;     //raycast has hit a grid
    private RaycastHit towHit;      //raycast has hit a tower
    private RaycastHit powerUpHit;  //raycast has hit a target or area

    //TOWER PROPERTIES
    private Transform towerContainer;   //gameobject that should hold all instantiated towers
    [HideInInspector]
    public TowerBase towerBase;         //tower base properties of floating tower
    [HideInInspector]
    public Upgrade upgrade;             //tower upgrade properties of floating tower
    [HideInInspector]
    public GameObject currentGrid;      //current grid the mouse is over
    [HideInInspector]
    public GameObject currentTower;     //current tower the mouse is over

    //indicating which button we clicked represented in the TowerManager tower index
    int index = 0;

    //GUI PROPERTIES
    public GameObject[] invisibleWidgets;
    
    //warning message text (empty at start)
    public Text errorText;

    //does this application run on mobile devices?
    //this variable determines the behaviour of touch inputs in the GUI implementation
    public bool mobile;


    //null reference checks and initialization of invisible widgets
    void Start()
    {
        //print errors if scripts are not set
        if (towerScript == null)
            Debug.LogWarning("GUI TowerManager not set");

        if (gridScript == null)
            Debug.LogWarning("GUI GridManager not set");

        if (powerUpScript == null)
            Debug.LogWarning("GUI PowerUpManager not set");

        //if this application runs on android or iphone set mobile boolean to true
        if (Application.platform == RuntimePlatform.Android ||
            Application.platform == RuntimePlatform.IPhonePlayer)
            mobile = true;

        //get tower container gameobject
        towerContainer = GameObject.Find("Tower Manager").transform;

        //store invisible widgets so they can be activated later
        foreach (GameObject gui_Obj in invisibleWidgets)
        {
            //disable widget
            gui_Obj.SetActive(false);
        }
    }


    //update is called every frame,
    //here we execute raycasts and input methods
    void Update()
    {
        //cast ray at our mouse position which then delivers input
        //for our RaycastHit 'towHit' and 'gridHit'
        ray = raycastCam.ScreenPointToRay(Input.mousePosition);

        //check if the ray hit a grid
        RaycastGrid();
        //check if the ray hit a tower
        RaycastTower();
        //check if the ray hit a powerup objective
        if(IsPowerUpSelected()) RaycastPowerUp();
    }


    //grid detection raycast
    //ignores grids if a tower is in front of them
    void RaycastGrid()
    {
        //if ray has hit a grid (we cast against gridMask with very large distance)
        //or a tower, because then the tower should be the first selection
        if (Physics.Raycast(ray, out gridHit, 300, SV.gridMask | SV.towerMask))
        {
            //show a visible line indicating the ray within the editor
            Debug.DrawLine(ray.origin, gridHit.point, Color.yellow);
            GameObject hit = gridHit.transform.gameObject;
            //check if hit object is on the grid layer
            //(no tower is between the raycast position)
            if(hit.layer == SV.gridLayer)
                currentGrid = gridHit.transform.gameObject;
        }
        else
            currentGrid = null;
    }


    //tower detection raycast
    void RaycastTower()
    {
        //if ray has hit a tower (we cast against towerMask with very large distance)
        if (Physics.Raycast(ray, out towHit, 300, SV.towerMask))
        {
            //show a visible line indicating the ray within the editor
            Debug.DrawLine(ray.origin, towHit.point, Color.red);

            currentTower = towHit.transform.gameObject;
        }
        else
            currentTower = null;
    }


    //deactivate all current selections
    //destroy floating tower if necessary, and free used variables
    public void CancelSelection(bool destroySelection)
    {
        //destroy floating tower on parameter destroySelection == true
        if (destroySelection && SV.selection)
            Destroy(SV.selection);

        currentGrid = null;     //free current grid
        currentTower = null;    //free current tower
        SV.selection = null;    //free tower selection (just to make sure we don't have an empty reference)
        SV.gridSelection = null;    //free grid selection (on BuildMode Grid) -"-
        gridScript.ToggleVisibility(false);     //disable grid visibility
    }


    //short way to check if a tower is already placed on the current grid
    public bool CheckIfGridIsFree()
    {
        if (currentGrid == null || gridScript.GridList.Contains(currentGrid.name))
            return false;
        else
            return true;
    }


    //set components of the tower passed in for later use and access
    public void SetTowerComponents(GameObject tower)
    {
        upgrade = tower.GetComponent<Upgrade>();
        towerBase = tower.GetComponent<TowerBase>();
    }


    //short way to check if there's an upgrade level left to upgrade to
    public bool AvailableUpgrade()
    {
        if (upgrade == null)
        {
            Debug.Log("Can't check for available upgrades, upgrade script isn't set.");
            return false;
        }

        //initialize boolean
        bool available = true;
        //cache current tower level
        int curLvl = upgrade.curLvl;
        //check against total upgrade levels
        if (curLvl >= upgrade.options.Count - 1)
            available = false;

        return available;
    }


    //method to check if the next upgrade level is affordable
    public bool AffordableUpgrade()
    {
        if (upgrade == null)
        {
            Debug.Log("Can't check for affordable upgrade, upgrade script isn't set.");
            return false;
        }

        //initialize boolean
        bool affordable = true;
        //cache current tower level
        int curLvl = upgrade.curLvl;
        //first check if there's an upgrade level left to upgrade to
        if (AvailableUpgrade())
        {
            //loop through resources
            for (int i = 0; i < GameHandler.resources.Length; i++)
            {
                //check if we can afford an upgrade to this tower
                if (GameHandler.resources[i] < upgrade.options[curLvl + 1].cost[i])
                {
                    affordable = false;
                    break;
                }
            }
        }
        else
            affordable = false;

        return affordable;
    }


    //return upgrade price for the next tower level upgrade
    public float[] GetUpgradePrice()
    {
        //initialize upgrade price array value for multiple resources
        float[] upgradePrice = new float[GameHandler.resources.Length];
        //cache current tower level
        int curLvl = upgrade.curLvl;
        //first check if there's an upgrade level left to upgrade to
        //else return an empty float array
        if (AvailableUpgrade())
        {
            //loop through resources and get direct cost
            for (int i = 0; i < upgradePrice.Length; i++)
            {
                upgradePrice[i] = upgrade.options[curLvl + 1].cost[i];
            }
        }

        return upgradePrice;
    }


    //return total price at which the selected tower gets sold
    public float[] GetSellPrice()
    {
        //initialize sell price array value for multiple resources
        float[] sellPrice = new float[GameHandler.resources.Length];
        //cache current tower level
        int curLvl = upgrade.curLvl;

        //loop through resources
        for (int i = 0; i < sellPrice.Length; i++)
        {
            //loop through each upgrade purchased for this tower
            //calculate new sell price based on all upgrades and sellLoss
            for (int j = 0; j < curLvl + 1; j++)
                sellPrice[i] += upgrade.options[j].cost[i] * (1f - (towerScript.sellLoss / 100f));
        }

        return sellPrice;
    }


    //create active selection / floating tower on pressing a tower button
    public void InstantiateTower(int clickedButton)
    {
        //store the TowerManager tower index passed in as parameter
        index = clickedButton;

        //we clicked one of these tower buttons
        //if we already have a floating tower, destroy it and free selections
        if (SV.selection)
        {
            currentGrid = null;
            currentTower = null;
            Destroy(SV.selection);
        }

        //check if there are free grids left
        //no free grid left (list count is equal to grid count)
        if (gridScript.GridList.Count == gridScript.transform.childCount)
        {
            //print a warning message
            StartCoroutine("DisplayError", "没有剩余的网格可以放塔!");
            Debug.Log("No free grids left for placing a new tower!");
            return;
        }

        //initialize price array with total count of resources
        float[] price = new float[GameHandler.resources.Length];
        //cache selected upgrade options for further processment 
        UpgOptions opt = towerScript.towerUpgrade[index].options[0];

        //loop through resources
        //get needed resources (buy price) of this tower from upgrade list
        for (int i = 0; i < price.Length; i++)
            price[i] = opt.cost[i];

        //check in case we have not enough resources left, abort purchase
        for (int i = 0; i < price.Length; i++)
        {
            if (GameHandler.resources[i] < price[i])
            {
                StartCoroutine("DisplayError", "没有足够的资源可以购买塔!");
                Debug.Log("Not enough resources for buying this tower!");
                //destroy selection. this is a bit hacky: CancelSelection() destroys all selections,
                //but we do want to keep our grid selection on BuildMode Grid, thus we cache it right
                //before calling this method and restore it afterwards
                if (SV.gridSelection)
                {
                    GameObject grid = SV.gridSelection;
                    CancelSelection(true);
                    SV.gridSelection = grid;
                    grid.renderer.enabled = true;
                }
                else
                    CancelSelection(true);
                return;
            }
        }

        //all checks went through, we are able to purchase this tower
        //instantiate selected tower from TowerManager prefab list and thus create a floating tower
        SV.selection = (GameObject)Instantiate(towerScript.towerPrefabs[index], SV.outOfView, Quaternion.identity);
        SV.selection.name = towerScript.towerNames[index];
        //get new base properties of this tower instance
        towerBase = SV.selection.GetComponentInChildren<TowerBase>();
        //change name of the gameobject holding this component to the defined one in TowerManager names list
        towerBase.gameObject.name = towerScript.towerNames[index];
        //parent tower to the container gameobject
        SV.selection.transform.parent = towerContainer;
        //get new upgrade properties of this tower instance
        upgrade = SV.selection.GetComponentInChildren<Upgrade>();
        towerBase.upgrade = upgrade;
        //disable its base properties, so while moving/placing the tower around, it can not attack
        towerBase.enabled = false;
        //show all grid renderers to see where we could (or could not) place the tower
        //but only highlight all of them on BuildMode Tower (where gridSelection is always empty)
        if (!SV.gridSelection)
            gridScript.ToggleVisibility(true);
        //apply passive powerups to this tower
        PowerUpManager.ApplyToSingleTower(towerBase, upgrade);
    }


    //enables a tower on a grid after buying it
    public void BuyTower()
    {
        //reduce resources by the tower price
        for (int i = 0; i < GameHandler.resources.Length; i++)
            GameHandler.resources[i] -= towerScript.towerUpgrade[index].options[0].cost[i];

        //add selected grid to the grid list so it is not available / free anymore
        if (SV.gridSelection)
        {
            currentGrid = SV.gridSelection;
            SV.gridSelection.renderer.enabled = false;
        }
        gridScript.GridList.Add(currentGrid.name);
        //change this grid color to the full-material for showing it is now occupied
        currentGrid.transform.renderer.material = gridScript.gridFullMat;
        //activate tower script and disable tower range indicator
        towerBase.enabled = true;
        towerBase.rangeInd.renderer.enabled = false;

        //get TowerRotation script and activate if it has one
        if (towerBase.turret)
            towerBase.turret.gameObject.GetComponent<TowerRotation>().enabled = true;
    }


    //upgrades the selected tower to the next level
    public void UpgradeTower()
    {
        //increase tower level and adjust range indicator properties
        upgrade.LVLchange();
        
        //reduce our resources
        for (int i = 0; i < GameHandler.resources.Length; i++)
            GameHandler.resources[i] -= upgrade.options[upgrade.curLvl].cost[i];
    }


    //sells the selected tower
    public void SellTower(GameObject tower)
    {
        float[] sellPrice = GetSellPrice();

        //add sell price to our resources
        for(int i = 0; i < sellPrice.Length; i++)
            GameHandler.resources[i] += sellPrice[i];    

        //define ray with down direction to get the grid beneath
        Ray ray = new Ray(tower.transform.position + new Vector3(0, 0.5f, 0), -transform.up);
        RaycastHit hit;

        //free grid the tower is standing on
        //raycast downwards of the tower against our grid mask to get the grid
        if (Physics.Raycast(ray, out hit, 20, SV.gridMask))
        {
            Transform grid = hit.transform;
            //remove grid from the "occupied"-list
            gridScript.GridList.Remove(grid.name);
            //change grid material to indicate it is free again
            grid.renderer.material = gridScript.gridFreeMat;
        }
    }


    //forward call to the PowerUpManager
    //set the active powerup based on an index
    public void SelectPowerUp(int index)
    {
        powerUpScript.SelectPowerUp(index);
    }


    //forward call to the PowerUpManager
    //set the passive powerup based on an index
    public void SelectPassivePowerUp(int index)
    {
        powerUpScript.SelectPassivePowerUp(index);
    }


    //forward call to the PowerUpManager
    //returns whether a powerup is set
    public bool IsPowerUpSelected()
    {
        if (powerUpScript)
            return powerUpScript.HasSelection();
        else
            return false;
    }


    //forward call to the PowerUpManager
    //returns whether a passive powerup is set
    public bool IsPassivePowerUpSelected()
    {
        if (powerUpScript)
            return powerUpScript.HasPassiveSelection();
        else
            return false;
    }


    //forward call to the PowerUpManager
    //returns the active powerup
    public BattlePowerUp GetPowerUp()
    {
        return powerUpScript.GetSelection();
    }


    //forward call to the PowerUpManager
    //returns the passive powerup
    public PassivePowerUp GetPassivePowerUp()
    {
        return powerUpScript.GetPassiveSelection();
    }


    //raycast against the world to detect where the
    //powerup instantiation should happen. Then
    //trigger powerup activation in PowerUpManager
    public void RaycastPowerUp()
    {
        //mask to raycast against
        int specificMask;
        BattlePowerUp powerup = powerUpScript.GetSelection();
        //unset previous raycast target
        powerup.target = null;

        //set raycast mask based on powerup type
        //for offensive powerups, we raycast against enemies
        //for defensive powerups, we raycast against towers
        if (powerup is OffensivePowerUp)
            specificMask = SV.enemyMask;
        else
            specificMask = SV.towerMask;

        //try to find a powerup target position in two steps:
        //first, detect if the user clicked on a specific gameobject,
        //then find the ground beneath this object and instantiate an area effect there.
        //second, directly locate the ground position if the user hasn't clicked on a tower/enemy
        if (Physics.Raycast(ray, out powerUpHit, 300f, specificMask))
        {
            //the user clicked on an object that is either an enemy or a tower
            //set the target transform
            Transform target = powerUpHit.transform;
            powerup.target = powerUpHit.transform;
            //cast a ray from that object towards the ground
            //to set the initial powerup position
            if (Physics.Raycast(target.position, Vector3.down, out powerUpHit, 100f, SV.worldMask))
            {
                if (powerUpHit.transform.CompareTag("Ground"))
                    powerup.position = powerUpHit.point;
                else
                    powerup.position = target.position;
            }
        }
        //the user has not clicked on an object,
        //we only use the ground hit position, otherwise do not execute the powerup
        else if (Physics.Raycast(ray, out powerUpHit, 300f, SV.worldMask))
        {
            if (!powerUpHit.transform.CompareTag("Ground"))
            {
                powerup.position = SV.outOfView;
                return;
            }

            powerup.position = powerUpHit.point;
        }

        //check against powerup requirements,
        //in case they are not met we set the powerup position out of view
        if (!powerup.CheckRequirements())
            powerup.position = SV.outOfView;
    }


    //forward call to the PowerUpManager
    //activate active powerup selection, if in sight
    public void ActivatePowerUp()
    {
        if(IsPowerUpSelected() && powerUpScript.GetSelection().position != SV.outOfView)
            powerUpScript.Activate();
    }
	
	
	//forward call to the PowerUpManager
    //activate passive powerup selection
    public bool ActivatePassivePowerUp()
    {
        if (!IsPassivePowerUpSelected()) return false;
        PassivePowerUp powerup = GetPassivePowerUp();

        bool affordable = true;
        //loop through resources
        for (int i = 0; i < GameHandler.resources.Length; i++)
        {
            //check if we can afford buying this powerup
            if (GameHandler.resources[i] < powerup.cost[i])
            {
                affordable = false;
                break;
            }
        }
        if (!affordable) return false;

        //affordable is true, substract resources
        for (int i = 0; i < powerup.cost.Length; i++)
            GameHandler.resources[i] -= powerup.cost[i];
        //activate powerup
        powerUpScript.ActivatePassive();
        return true;
    }


    //forward call to the PowerUpManager
    //clear active powerup selection
    public void DeselectPowerUp()
    {
        powerUpScript.Deselect();
    }


    //volume value callback method, called by slider located under
    //UI Root (2D) > Camera > Anchor_Menu > Panel_Main > Slider
    //simply sets the volume of the AudioListener (Camera) in the scene
    public void OnVolumeChange(float value)
    {
        AudioListener.volume = value;
    }


    //pass in any text to this method and it will draw a message on the screen
    //used to show hints and warnings for the player when all grids are occupied,
    //not enough money to buy tower and similiar actions
    public IEnumerator DisplayError(string text)
    {
        //errorText is equal to passed text, do not execute again and return instead
        if (text == errorText.text)
            yield break;

        //set errorText to the passed in message text
        errorText.text = text;
        //start fading in the text
        StartCoroutine("FadeIn", errorText.gameObject);

        //show this message for 2 seconds
        yield return new WaitForSeconds(2);

        //if the error text has changed when we first set it,
        //(another message occured during the first one) we return at this position
        if (text != errorText.text)
            yield break;

        //start fading out the text
        StartCoroutine("FadeOut", errorText.gameObject);
        //wait fade out delay
        yield return new WaitForSeconds(0.2f);

        //after 2 seconds and when the text hasn't changed since then,
        //(this prevents that the second text would only display for less than 2 seconds)
        //reset/empty this text so no message gets shown anymore
        errorText.text = "";
    }


    //fade in widgets passed in
    public IEnumerator FadeIn(GameObject gObj)
    {
        //fade in within 0.2 seconds
        float duration = 0.2f;

        //if widget is not active, activate it
        //if the widget is already active, we don't have to fade it in
        if (!gObj.activeInHierarchy)
            gObj.SetActive(true);
        else
            yield break;

        //create alpha value
        float alpha = 1f;

        Graphic[] graphics = gObj.GetComponentsInChildren<Graphic>(true);

        //loop through widgets and set their alpha value to 1 using a color tween
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].canvasRenderer.SetAlpha(0f);
            graphics[i].CrossFadeAlpha(alpha, duration, true);
        }
    }


    //fade out widgets passed in
    public IEnumerator FadeOut(GameObject gObj)
    {
        //fade out within 0.2 seconds
        float duration = 0.2f;

        //if gameobject is already inactive, do nothing
        if (!gObj.activeInHierarchy)
            yield break;

        //create alpha value
        float alpha = 0f;

        Graphic[] graphics = gObj.GetComponentsInChildren<Graphic>(true);

        //loop through widgets and set their alpha value to 0 using a color tween
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].CrossFadeAlpha(alpha, duration, true);
        }

        //wait till fade out was complete
        yield return new WaitForSeconds(duration);

        //disable UI elements
        gObj.SetActive(false);
    }
}
