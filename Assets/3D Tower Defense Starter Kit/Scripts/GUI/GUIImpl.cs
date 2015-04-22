/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

//implementation of the GUILogic.cs script with customized behavior
public class GUIImpl : MonoBehaviour
{
    //reference to the main logic
    private GUILogic gui;
    
    //BuildMode option for placing towers
    public enum BuildMode
    {
        Tower, //choose tower first, then place on grid
        Grid    //choose grid first, then select tower
    }
    //selected BuildMode option
    public BuildMode buildMode = BuildMode.Tower;

    //PARTICLE FX
    public GameObject buildFx;
    public GameObject upgradeFx;

    //GUI ELEMENTS
    public pan panels = new pan();
    public btn buttons = new btn();
    public lbl labels = new lbl();
    public snd sound = new snd();
    public con control = new con();

    //time value to insert a delay between mouse clicks
    //(we do not want to show the upgrade menu instantly after a tower was bought)
    private float time;

    //camera starting rotation, only needed on mobiles to reset the camera after leaving a tower
    private Vector3 initRot;
    //selected toggle after first press, needed on two-click actions
    //(on mobiles for tower selection, desktop for powerup de/selection
    private Toggle selectedCheckbox = null;
    private Toggle selectedPowerup = null;


    //get reference of the main GUI script
    void Awake()
    {
        gui = GetComponent<GUILogic>();
    }


    void Start()
    {
        //set camera starting rotation (mobile or not)
        initRot = gui.raycastCam.transform.eulerAngles;

        //instantiate self control mouse position indicator at a non visible position
        //so SelfControl.cs has access to it and can change its position when we right click on a tower to control it
        control.crosshair = (GameObject)Instantiate(control.crosshair, SV.outOfView, Quaternion.identity);
        //the same with AimPrefab, it indicates the flight path of projectiles of a tower
        control.aimIndicator = (GameObject)Instantiate(control.aimIndicator, SV.outOfView, Quaternion.identity);

        //subscribe to the "power up fired" event of PowerUpManager, for calling PowerUpActivated()
        PowerUpManager.battlePowerUpActivated += PowerUpActivated;
    }


    void OnDestroy()
    {
        //unsubscribe from events
        PowerUpManager.battlePowerUpActivated -= PowerUpActivated;
    }


    //show all tower buttons
    public void ShowButtons()
    {
        //if the exit menu is active, do nothing
        if (panels.main.activeInHierarchy)
            return;

        //fade out or fade in the tower buttons depending on their visibility
        gui.StartCoroutine("FadeOut", buttons.towerButtons);
        buttons.towerButtons.SetActive(true);

        //disable tooltip, upgrade panel
        DisableMenus();
    }


    //fade out tooltip and upgrade panel and deactivate all current selections
    public void DisableMenus()
    {
        //toggle upgrade menu visibility value
        SV.showUpgrade = false;
        CancelInvoke("UpdateUpgradeMenu");

        //fade out tooltip and upgrade panel if we don't show a power up description
        if(!gui.IsPowerUpSelected())
            gui.StartCoroutine("FadeOut", panels.tooltip);
        gui.StartCoroutine("FadeOut", panels.upgradeMenu);
        //deselect selected tower buttons in case they have a UIToggle component
        selectedCheckbox = null;
        Toggle[] allToggles = buttons.towerButtons.GetComponentsInChildren<Toggle>(true);
        for (int i = 0; i < allToggles.Length; i++)
            allToggles[i].isOn = false;
        panels.passivePowerUps.SetActive(false);

        //disable range indicator and selected grid visibility if set
        if (gui.towerBase) gui.towerBase.rangeInd.renderer.enabled = false;
        if (SV.gridSelection) SV.gridSelection.renderer.enabled = false;

        //destroy current selections and free variables
        gui.CancelSelection(true);
    }


    //update is called every frame,
    //here we check if the user pressed the escape button (desktop or mobile)
    //and react to GUILogic.cs raycasts
    void Update()
    {
        CheckESC();

        //don't check against grids or towers (below) if our mouse is over the gui
        //and we haven't selected a grid (only on BuildMode Grid) or the main menu is shown.
        //on mobiles, IsPointerOverEventSystemObject is always null
        if (EventSystem.current.IsPointerOverGameObject() && !SV.gridSelection || SV.showExit)
            return;
        //one by one: if we performed a click and do not control a tower or powerup,
        //and our mouse is not over a tower OR the tower is not the selected one
        //and our mouse is not over a grid OR the grid is not the selected one (in BuildMode Grid)
        else if (Input.GetMouseButtonUp(0) && !SV.control && !gui.IsPowerUpSelected()
            && (!gui.currentTower || SV.selection && gui.currentTower.transform.parent.gameObject != SV.selection)
            && (!gui.currentGrid || SV.showUpgrade && gui.currentGrid || buildMode == BuildMode.Grid
                && SV.selection && gui.currentGrid != SV.gridSelection))
        {
            //if all of this went through, lastly check if we're not about to
            //press a button. in this case raycasting a gui element will return true
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            //THEN we obviously clicked in an empty space in the game,
            //this means disabling all menus on the screen and cancel active selections
            DisableMenus();
            //on BuildMode Grid additionally unset active tower button selections (if any)
            //and fade out tower buttons
            if (buildMode == BuildMode.Grid)
                gui.StartCoroutine("FadeOut", buttons.towerButtons);
        }

        //react to GUILogic.cs raycasts
        ProcessGrid();
        ProcessTower();
        ProcessPowerUps();
    }


    //checks if the user presses the esc button and
    //displays the exit menu
    void CheckESC()
    {
        //on ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //if we control a tower
            if (SV.control)
            {
                //remove control mechanism
                DisableSelfControl();
            }

            //if exit menu isn't already active display it
            gui.StartCoroutine("FadeIn", panels.main);
            //fade out all tower buttons
            gui.StartCoroutine("FadeOut", buttons.towerButtons);
            //disable active selections
            DisableMenus();
            //disable active powerup
            if(gui.IsPowerUpSelected())
                DeselectPowerUp();
            //toggle exit menu visibility variable
            SV.showExit = true;
            //pause the game
            Time.timeScale = 0.0001f;
        }
    }


    void ProcessGrid()
    {
        //differ between BuildModes
        //when processing grid raycasts
        switch (buildMode)
        {
            //select tower first
            case BuildMode.Tower:
            {
                //we don't have a floating tower ready for purchase, return
                if (!SV.selection)
                    return;
                else if (!gui.CheckIfGridIsFree() || gui.currentTower
                         && gui.currentTower.transform.parent.gameObject != SV.selection)
                {
                    //grid is contained in our occupied grid list and therefore not available
                    //or the mouse is currently hovering over another tower than our selection.
                    //we can't place our tower on this grid, move our tower out of sight
                    //so it does not get rendered by the camera anymore
                    SV.selection.transform.position = SV.outOfView;
                 }
                 else
                 {
                    //the targeted grid is available
                    //place tower on top of this grid
                    SV.selection.transform.position = gui.currentGrid.transform.position;

                    //rotate turret to match the grid rotation, without affecting its child
                    if (gui.towerBase.turret)
                        gui.towerBase.turret.localRotation = gui.currentGrid.transform.rotation;

                    //we bought a tower by pressing the left mouse button while over the grid
                    if (Input.GetMouseButtonUp(0))
                    {
                        //--> purchase successful
                        BuyTower();
                    }
                }
                break;
            }

            //select grid first
            case BuildMode.Grid:
            {
                //don't register grid selections if:
                //a tower is controlled by the player or a powerup has been selected 
                if (SV.control || gui.IsPowerUpSelected())
                    return;

                //the player performed a click on an available grid,
                //while not placing a tower already
                if (Input.GetMouseButtonUp(0) && gui.CheckIfGridIsFree() && !SV.selection)
                {
                    //hide previous grid if we switched over to another one
                    if(SV.gridSelection)
                        SV.gridSelection.renderer.enabled = false;
                    //cache current grid the mouse is over
                    GameObject grid = gui.currentGrid;
                    //show tower buttons if not already done
                    if (!buttons.towerButtons.activeInHierarchy)
                        ShowButtons();
                    //set current grid to selected one and highlight it
                    SV.gridSelection = grid;
                    SV.gridSelection.renderer.enabled = true;
                }
                //display tower buttons at grid position
                if (SV.gridSelection)
                {
                    Vector3 pos = SV.gridSelection.transform.position;
                    RepositionTowerButtons(pos);
                }
                break;
            }
        }
    }


    void ProcessTower()
    {
        //get current tower the mouse is over
        GameObject tower = gui.currentTower;
        //don't continue on active placement or without tower
        if (SV.selection || tower == null) return;

        //if we select a tower for purchase, and left click - while over a free grid - to place/buy it,
        //this would open the upgrade menu instantly (because the click on this tower is also recognized as upgrade click)
        //so we check whether between those (two) clicks some time has passed (a half second) and only then open the the upgrade menu. 
        //also we do not want to open the upgrade menu if we control a tower or want to activate a powerup
        if (Input.GetMouseButtonUp(0) && time + 0.5f < Time.time && !SV.control && !gui.IsPowerUpSelected())
        {
            //finally, show upgrade menu of this tower
            ShowUpgradeMenu(tower);
        }

        //get time stamp of right mouse button, used below
        if (Input.GetMouseButtonDown(1))
        {
            time = Time.time;
        }

        //check whether we released the right mouse button soon enough to simulate a simple right click
        //(release less than a half second) and enable self control mode
        //on mobile devices we don't use the right mouse button, instead we use a simple tap on a tower
        //when the upgrade menu of this tower is already shown - this simulates that we need a double tap
//        if ((Input.GetMouseButtonUp(1) && time + 0.5f > Time.time)
//            || gui.mobile && Input.GetMouseButtonDown(0) && SV.showUpgrade && gui.upgrade.gameObject == tower)
//        {
            //attach self control script to this tower
//            EnableSelfControl();
//        }
    }


    void ProcessPowerUps()
    {
        //try to launch a power up
        //if we selected one and left clicked
        if (gui.IsPowerUpSelected())
        {           
            //reposition highlighted powerup prefab, if set
            //this will snap to enemys, towers or ground
            if (SV.powerUpIndicator)
                SV.powerUpIndicator.transform.position = gui.GetPowerUp().position + new Vector3(0, 0.25f, 0);

            //try to execute powerup on mouse click
            if (Input.GetMouseButtonUp(0))
                gui.ActivatePowerUp();
        }
    }


    //instantiate floating tower when a tower button was clicked
    public void CreateTower(int index)
    {
        Transform button = buttons.towerButtons.transform.GetChild(index);
		Toggle checkbox = button.GetComponent<Toggle>();
		if (selectedCheckbox != null && !checkbox.isOn && selectedCheckbox != checkbox) return;

        if (buildMode == BuildMode.Grid && !SV.gridSelection)
        {
                Debug.LogWarning("Tried to create tower on BuildMode Grid, without selecting a grid."
                         + " This won't work, aborting. Design your GUI to select a grid first (see the mobile scene)!");
                return;
        }

        //the upgrade menu is shown, disable this menu and the range indicator
        if (SV.showUpgrade && gui.towerBase)
        {
            gui.towerBase.rangeInd.renderer.enabled = false;
            SV.showUpgrade = false;
            CancelInvoke("UpdateUpgradeMenu");
        }
	
        //only instantiate floating tower on an active toggle
        //use toggle value (0 = first tower, 1 = second tower in the list etc.)
        if (!checkbox || checkbox.isOn)  gui.InstantiateTower(index);
        //show tool tip menu
        ShowTooltipMenu(index);

        //if a tower was created, toggle range indicator visibility on and continue
        //else unselect tower buttons (on BuildMode Grid) and return
        if (SV.selection)
            gui.towerBase.rangeInd.renderer.enabled = true;

        //clear active powerup selection
        if(gui.IsPowerUpSelected())
            DeselectPowerUp();

        if (buildMode == BuildMode.Grid)
        {
            //place tower on top of the selected grid
            if (SV.selection)
            {
                SV.selection.transform.position = SV.gridSelection.transform.position;

                //rotate turret to match the grid rotation, without affecting its child
                if (gui.towerBase.turret)
                    gui.towerBase.turret.localRotation = SV.gridSelection.transform.rotation;
            }
        
            //if the player selects the same tower button twice, this triggers
            //the actual tower purchase (first press returns true, second = false)
            if (checkbox.isOn)
                selectedCheckbox = checkbox;
            else if (SV.selection && !checkbox.isOn && checkbox == selectedCheckbox)
            {
                //buy tower and hide tower buttons again
                gui.StartCoroutine("FadeOut", buttons.towerButtons);
                BuyTower();
            }

            //disable all other toggles
            Toggle[] allToggles = buttons.towerButtons.GetComponentsInChildren<Toggle>(true);
            for (int i = 0; i < allToggles.Length; i++)
                if (allToggles[i] != selectedCheckbox) allToggles[i].isOn = false;
        }
    }


    //buy floating tower and place it on a grid
    void BuyTower()
    {
        //play tower bought sound
        AudioManager.Play2D(sound.build);

        //let PoolManager spawn a build fx prefab at the placed tower
        if(buildFx)
            PoolManager.Pools["Particles"].Spawn(buildFx, SV.selection.transform.position, Quaternion.identity);

        //buy selected tower
        gui.BuyTower();

        //free active selections
        gui.CancelSelection(false);

        //disable range indicator visibility if set
        if (gui.towerBase) gui.towerBase.rangeInd.renderer.enabled = false;

        //fade out tooltip panel
        gui.StartCoroutine("FadeOut", panels.tooltip);

        //set current time to prevent a simulated double click, see above in ProcessTower()
        time = Time.time;

    }


    public void SellTower()
    {
        //we sold this tower
        //play tower sold sound
        AudioManager.Play2D(sound.sell);

        //get selected tower, for that the upgrade menu is active
        GameObject tower = gui.upgrade.transform.parent.gameObject;
        
        //sell the selected tower, add resources
        gui.SellTower(tower);
        //highlight this tower as active selection
        SV.selection = tower;
        //frees the selection by destroying the selected tower
        DisableMenus();
    }


    //show upgrade menu for the passed tower
    public void ShowUpgradeMenu(GameObject tower)
    {
        //enable tooltip and upgrade panel
        gui.StartCoroutine("FadeIn", panels.tooltip);
        gui.StartCoroutine("FadeIn", panels.upgradeMenu);
        //toggle upgrade menu visibility variable
        SV.showUpgrade = true;

        //disable previous range indicator if one is set already (old one
        //from another tower) and free grid selection (on BuildMode Grid)
        if (gui.towerBase)
            gui.towerBase.rangeInd.renderer.enabled = false;
        if (SV.gridSelection)
        {
            SV.gridSelection.renderer.enabled = false;
            gui.StartCoroutine("FadeOut", buttons.towerButtons);
        }
        SV.gridSelection = null;

        //set tower properties
        gui.SetTowerComponents(tower);
        //show tower range indicator
        gui.towerBase.rangeInd.renderer.enabled = true;
        
        //refresh values instantly
        UpdateUpgradeMenu();
        //periodically refresh the upgrade tooltip with current values 
        if(!IsInvoking("UpdateUpgradeMenu"))
            InvokeRepeating("UpdateUpgradeMenu", .5f, 1f);
    }


    void UpdateUpgradeMenu()
    {
        //store current upgrade level
        int curLvl = gui.upgrade.curLvl;
        //store necessary upgrade option info for later use
        UpgOptions upgOptions = gui.upgrade.options[curLvl];

        //set UI label properties with information of this tower
        labels.headerName.text = gui.upgrade.gameObject.name;
        labels.properties.text = "等级:" + "\n" +
                                "范围:" + "\n" +
                                "伤害:" + "\n" +
                                "延迟:" + "\n" +
                                "目标:";
        //visualize current tower stats
        //round floats to 2 decimal places
        labels.stats.text = +curLvl + "\n" +
                                (Mathf.Round(upgOptions.radius * 100f) / 100f) + "\n" +
                                (Mathf.Round(upgOptions.damage * 100f) / 100f) + "\n" +
                                (Mathf.Round(upgOptions.shootDelay * 100f) / 100f) + "\n" +
                                upgOptions.targetCount;

        //visualize tower stats on the next level if the label was set,
        //and round floats to 2 decimal places
        if (labels.upgradeInfo)
        {
            //check if we have a level to upgrade left
            if (curLvl < gui.upgrade.options.Count - 1)
            {
                //get upgrade option for the next level and display stats
                UpgOptions nextUpg = gui.upgrade.options[curLvl + 1];
                labels.upgradeInfo.text = "= " + (curLvl + 1) + "\n" +
                                          "= " + (Mathf.Round(nextUpg.radius * 100f) / 100f) + "\n" +
                                          "= " + (Mathf.Round(nextUpg.damage * 100f) / 100f) + "\n" +
                                          "= " + (Mathf.Round(nextUpg.shootDelay * 100f) / 100f) + "\n" +
                                          "= " + nextUpg.targetCount;
            }
            else
                //don't display anything on the last level
                labels.upgradeInfo.text = "";
        }

        //initialize upgrade and sell price array value for multiple resources
        float[] sellPrice = gui.GetSellPrice();
        float[] upgradePrice = gui.GetUpgradePrice();
        //only display the upgrade button, if there IS a level for upgrading left
        //and we have enough money for upgrading to the next level, check every resource
        //initialize boolean as true
        bool affordable = true;

        for (int i = 0; i < upgradePrice.Length; i++)
        {
            //set sell price resource label to the actual value
            labels.sellPrice[i].text = "$" + sellPrice[i];

            //check if we can buy another upgrade level
            //if not, erase price values
            if (!gui.AvailableUpgrade())
            {
                affordable = false;
                labels.price[i].text = "";
                continue;
            }

            //set price label for upgrading to the next level
			labels.price[i].text = "￥" + upgradePrice[i];
        }

        //there is a level to upgrade left, so check if we can afford this tower upgrade
        if (affordable)
            affordable = gui.AffordableUpgrade();

        //the upgrade is still affordable
        if (affordable)
            //in case the upgrade button was deactivated we activate it here again
            buttons.button_upgrade.SetActive(true);
        else
            //we can't afford an upgrade, disable upgrade button
            buttons.button_upgrade.SetActive(false);
    }


    //upgrade tower
    public void Upgrade()
    {
        //tower upgrade successful
        //play tower upgrade sound
        AudioManager.Play2D(sound.upgrade);

        GameObject tower = gui.upgrade.gameObject;

        //let PoolManager spawn upgrade fx prefab at the tower position
        if (upgradeFx)
            PoolManager.Pools["Particles"].Spawn(upgradeFx, tower.transform.position, Quaternion.identity);

        //execute upgrade
        gui.UpgradeTower();

        //refresh upgrade panel with new values
        UpdateUpgradeMenu();
    }


    //enable tooltip menu and disable upgrade menu if active
    public void ShowTooltipMenu(int index)
    {

        //fade in tooltip panel, fade out upgrade panel
        gui.StartCoroutine("FadeIn", panels.tooltip);
        gui.StartCoroutine("FadeOut", panels.upgradeMenu);
        //toggle upgrade menu visibility value
        SV.showUpgrade = false;
        CancelInvoke("UpdateUpgradeMenu");

        TowerBase baseOptions = null;
        UpgOptions upgOptions = null;
        //store tower base properties from TowerManager lists,
        //also store necessary upgrade option info for later use
        //(in case we haven't instantiated a tower because it
        //wasn't affordable, we can't access an instance and
        //have to use these components pre-stored in TowerManager.cs)
        if (SV.selection)
        {
            baseOptions = gui.towerBase;
            upgOptions = gui.upgrade.options[0];
        }
        else
        {
            baseOptions = gui.towerScript.towerBase[index];
            upgOptions = gui.towerScript.towerUpgrade[index].options[0];
        }

        //set all information related to tower properties,
        //such as tower name, properties and initial price
        labels.headerName.text = gui.towerScript.towerNames[index];
        labels.properties.text = "子弹:" + "\n" +
                                "范围:" + "\n" +
                                "伤害:" + "\n" +
                                "延迟:" + "\n" +
                                "目标:";
        //visualize current tower stats
        labels.stats.text = baseOptions.projectile.name + "\n" +
                                upgOptions.radius + "\n" +
                                upgOptions.damage + "\n" +
                                upgOptions.shootDelay + "\n" +
                                baseOptions.myTargets;

        //set visible label price text for each resource
        for (int i = 0; i < GameHandler.resources.Length; i++)
			labels.price[i].text = "￥" + upgOptions.cost[i];
    }


    //game exit menu
    public void ExitMenu(int index)
    {
        //toggle exit menu visibility
        SV.showExit = false;
        //resume game
        Time.timeScale = 1;

        //no matter what we clicked (main menu or cancel),
        //we hide the exit menu again
        gui.StartCoroutine("FadeOut", panels.main);

        //handle exit button
        if (index == 1)
        {
            //load our first scene - the main menu
            Application.LoadLevel(0);
        }
    }


    //attach self control component to the selected tower
    public void EnableSelfControl()
    {
        //we right-clicked/double tapped the tower we control already,
        //or the main menu is shown, do nothing (return)
        if (SV.control || SV.showExit || gui.IsPowerUpSelected())
            return;

        //disable the main tower button and all tower buttons
        //while controlling a tower we don't want them to be visible
        if(buttons.mainButton) buttons.mainButton.SetActive(false);
        gui.StartCoroutine("FadeOut", buttons.towerButtons);
        
		//also disable power up buttons
        if (buttons.powerUpButtons) buttons.powerUpButtons.SetActive(false);
        if (buttons.button_showPassive) buttons.button_showPassive.SetActive(false);
        
		//free grid selection (on BuildMode Grid)
        if (SV.gridSelection)
            SV.gridSelection.renderer.enabled = false;
        SV.gridSelection = null;

        //ensure to disable upgrade menu and old range indicator if they're enabled
        if (SV.showUpgrade)
        {
            //fade out tooltip and upgrade menu
            gui.StartCoroutine("FadeOut", panels.tooltip);
            gui.StartCoroutine("FadeOut", panels.upgradeMenu);
            //toggle upgrade menu visibility
            SV.showUpgrade = false;
            CancelInvoke("UpdateUpgradeMenu");
            //hide tower range indicator
            gui.towerBase.rangeInd.renderer.enabled = false;
        }

        //add control component to the desired tower and cache it
        SV.control = gui.currentTower.transform.gameObject.AddComponent<SelfControl>();
        //initialize self control variables
        SV.control.Initialize(gameObject, control.crosshair, control.aimIndicator,
                              control.towerHeight, gui.mobile);
        //update components to controlled tower
        gui.SetTowerComponents(gui.currentTower);
        //disable active idle animation, if set
        if(gui.towerBase.idleAnim)
            gui.towerBase.animation.Stop(gui.towerBase.idleAnim.name);
        //check remaining reload time for this tower
        StartCoroutine("DrawReload");

        //enable mobile shot button (in mobile mode)
        if (gui.mobile)
            buttons.mobile_shoot.SetActive(true);

        //enable exit tower button
        panels.control.SetActive(true);
    }


    //only mobile, shoot button for self control script
    public void InteractiveShoot()
    {
        SV.control.Attack();
    }


    //terminate interactive tower control
    public void DisableSelfControl()
    {
        //remove control mechanism    
        SV.control.Terminate();
        //stop manipulating the reloading slider value
        StopCoroutine("DrawReload");

        //set main tower button, all tower and power up buttons back to active
        panels.control.SetActive(false);
        if(buttons.mainButton) buttons.mainButton.SetActive(true);
        if(buttons.powerUpButtons) buttons.powerUpButtons.SetActive(true);
        if (buttons.button_showPassive) buttons.button_showPassive.SetActive(true);

        //on mobile devices, hide the mobile shot button again
        //and reset the camera to the initial rotation using HOTween
        if (gui.mobile)
        {
            buttons.mobile_shoot.SetActive(false);
            Holoville.HOTween.HOTween.To(gui.raycastCam.transform, 1f, "rotation", initRot);
        }
    }


    //reposition tower buttons to active grid selection on BuildMode Grid
    void RepositionTowerButtons(Vector3 pos)
    {
        //cache tower buttons transform and UI camera for quick access
        Transform towerBtns = buttons.towerButtons.transform;
        Camera cam = Camera.main;
        //convert grid world position to screen position
        Vector3 mPos = cam.WorldToScreenPoint(pos);
        mPos.z = 0;

        //don't continue without UI camera
        if (cam != null)
        {          
            //the grid position exceeds our game view, abort selection
            if (mPos.x < 0 || mPos.x > Screen.width || mPos.y < 0 || mPos.y > Screen.height)
                ShowButtons();

            //update the absolute buttons position and save the local one
            towerBtns.position = mPos;
            mPos = towerBtns.localPosition;
            //round to pixel perfect positioning
            mPos.x = Mathf.Round(mPos.x);
            mPos.y = Mathf.Round(mPos.y);
            towerBtns.localPosition = mPos;
        }
    }


    //animate frames of reload texture. Gets started by SelfControl.cs in Attack()
    //if the controlled tower attacked
    public IEnumerator DrawReload()
    {
        //divide delay / total frames so we get waiting time after each frame
        //in order to complete it within the given delay time
        //cache delay for shooting at this level
        int curLvl = gui.upgrade.curLvl;
        float shootDelay = gui.upgrade.options[curLvl].shootDelay;

        //calculate time when this tower can shoot again
        float remainTime = gui.towerBase.lastShot + shootDelay - Time.time;
        if (remainTime < 0) remainTime = 0;

        //cache slider, get and set current reload time in % to the slider
        Slider slider = control.relSlider;
        float curValue = 1 - (remainTime / shootDelay);
        slider.value = curValue;
        control.relSprite.color = Color.yellow;

        //remainTime > 0 means this tower shot already within the last few seconds,
        //so we change the color indicating a reload
        if (curValue == 1)
        {
            //there is no need to animate the slider,
            //set color to finished one and break out here
            control.relSprite.color = Color.green;
            yield break;
        }

        //lerping time value
        //as long as the bar isn't full
        float t = 0f;
        while (t < 1)
        {
            if (shootDelay != gui.upgrade.options[curLvl].shootDelay)
            {
                shootDelay = gui.upgrade.options[curLvl].shootDelay;
                remainTime = gui.towerBase.lastShot + shootDelay - Time.time;
                if (remainTime < 0) remainTime = 0;
                t = 0;
            }

            //increase time value depending on remaining reload time
            t += Time.deltaTime / remainTime;
            //set slider value by lerping from current value to 1 = full
            slider.value = t;
            yield return null;
        }

        //the bar is full - we can shoot again, change bar color to finished
        control.relSprite.color = Color.green;
    }


    //select / deselect power up
    public void SelectPowerUp(int index)
    {
        //if the exit menu is active, do nothing
        if (panels.main.activeInHierarchy)
            return;

        Transform button = buttons.powerUpButtons.transform.GetChild(index);
        Toggle checkbox = button.GetComponent<Toggle>();

        if (checkbox.isOn || checkbox == selectedPowerup)
        {
            selectedPowerup = checkbox;
            gui.SelectPowerUp(index);
        }
        else
            return;

        //toggle all (but not the selected) power up buttons to false 
        Toggle[] allToggles = buttons.powerUpButtons.GetComponentsInChildren<Toggle>(true);
        for (int i = 0; i < allToggles.Length; i++)
            if(allToggles[i] != checkbox)
                allToggles[i].isOn = false;

        //since we handle both selecting and deselecting through this method,
        //here we check if we deselected a power up and disable the active selection
        if (!gui.IsPowerUpSelected())
        {
            DeselectPowerUp();
            return;
        }

        //these are the same method calls as in DisableMenus(),
        //but without hiding the tooltip menu, instead we fade it in
        SV.showUpgrade = false;
        CancelInvoke("UpdateUpgradeMenu");
        gui.StartCoroutine("FadeIn", panels.tooltip);
        gui.StartCoroutine("FadeOut", panels.upgradeMenu);

        //disable range indicator visibility if set
        if (gui.towerBase) gui.towerBase.rangeInd.renderer.enabled = false;
        if (SV.gridSelection) SV.gridSelection.renderer.enabled = false;
        //hide tower buttons
        if (buildMode == BuildMode.Grid)
            gui.StartCoroutine("FadeOut", buttons.towerButtons);
        gui.CancelSelection(true);

        //get the selected power up
        //here we re-use the tooltip menu (used for displaying tower stats)
        //for displaying the power up name and description
        BattlePowerUp powerUp = gui.GetPowerUp();
        labels.headerName.text = powerUp.name;
        labels.properties.text = powerUp.description.Replace("\\n", "\n");
        labels.stats.text = "";
        labels.price[0].text = "";

        //manage powerup indicator prefab
        //if there's an active one in the scene already, despawn it
        if (SV.powerUpIndicator)
            PoolManager.Pools["Particles"].Despawn(SV.powerUpIndicator);
        SV.powerUpIndicator = null;
        //check if the current powerup has a prefab for highlighting
        if (powerUp.indicator)
        {
            //instantiate the indicator prefab at a non-visible position
            SV.powerUpIndicator = PoolManager.Pools["Particles"].Spawn(powerUp.indicator,
                                  SV.outOfView, Quaternion.identity);
            //get the maximum area size of the powerup and scale the indicator accordingly
            float size = powerUp.GetMaxRadius() * 2;
            if (size > 0)
                SV.powerUpIndicator.transform.localScale = Vector3.one * size;
        }
    }


    //trigger cooldown timer on power up activation
    //called via PowerUpManager's action event
    void PowerUpActivated(BattlePowerUp powerUp)
    {
        //if the power up has a label for displaying a timer
        if (powerUp.timerText)
            StartCoroutine("Timer", powerUp);
        //delesect active selection
        DeselectPowerUp();
    }


    //clear power up selection and fade out its description
    void DeselectPowerUp()
    {
        //unset active powerup area indicator
        if (SV.powerUpIndicator)
            PoolManager.Pools["Particles"].Despawn(SV.powerUpIndicator);
        SV.powerUpIndicator = null;
        //clear selection
        gui.DeselectPowerUp();
        selectedPowerup = null;
        
        //toggle all (and the selected) power up buttons to false 
        Toggle[] allToggles = buttons.powerUpButtons.GetComponentsInChildren<Toggle>(true);
        for (int i = 0; i < allToggles.Length; i++)
            allToggles[i].isOn = false;
        
        //in case we don't want to place a tower next,
        //also fade out the tooltip menu
        if(!SV.selection)
            gui.StartCoroutine("FadeOut", panels.tooltip);
    }


    //this method reduces a seconds variable till time is up,
    //while this value gets displayed on the screen
    IEnumerator Timer(BattlePowerUp powerUp)
    {
        //store passed in seconds value and add current playtime
        //to get the targeted playtime value
        float timer = Time.time + powerUp.cooldown;

        //while the playtime hasn't reached the desired playtime
        while (Time.time < timer)
        {
            //get actual seconds left by subtracting calculated and current playtime
            //this value gets rounded to two decimals
            powerUp.timerText.text = "" + Mathf.CeilToInt(timer - Time.time);
            yield return true;
        }

        //when the time is up we clear the text displayed on the screen
        powerUp.timerText.text = "";
    }
	
	
	//show passive powerup panel
    public void ShowPassivePowerUps()
    {
        //if the exit menu is active, do nothing
        if (panels.main.activeInHierarchy)
            return;

        panels.passivePowerUps.SetActive(true);
        if(!gui.IsPassivePowerUpSelected())
            buttons.button_buyPassive.SetActive(false);
    }


    //select passive powerup when clicking on an icon in the panel
    public void SelectPassivePowerUp(int index)
    {
        //if the exit menu is active, do nothing
        if (panels.main.activeInHierarchy)
            return;

        //try to set the current passive selection
        gui.SelectPassivePowerUp(index);

        //display selected powerup properties
        PassivePowerUp powerup = gui.GetPassivePowerUp();
        labels.passivePUName.text = powerup.name;
        labels.passivePUDescription.text = powerup.description;
        for (int i = 0; i < powerup.cost.Length; i++)
            labels.passivePUPrice[i].text = "$" + powerup.cost[i];

        //show or hide the buy button based on its state
        if (powerup.enabled || powerup.locked)
            buttons.button_buyPassive.SetActive(false);
        else
            buttons.button_buyPassive.SetActive(true);
    }


    //activate passive powerup by clicking on the buy button
    public void ActivatePassivePowerUp()
    {
        //buy powerup and hide buy button with enough resources,
        //otherwise display message that the user doesn't have enough resources
        if (gui.ActivatePassivePowerUp())
            buttons.button_buyPassive.SetActive(false);
        else
            StartCoroutine(gui.DisplayError("Not enough resources for this powerup."));
    }


    //PANEL, BUTTON, LABEL elements
    //SOUND effects and SELF CONTROL variables

    [System.Serializable]
    public class pan
    {
        public GameObject main;         //exit menu panel
        public GameObject upgradeMenu;  //upgrade menu panel
        public GameObject tooltip;      //tooltip menu panel
		public GameObject passivePowerUps; //passive powerups panel
        public GameObject control;      //self control panel
    }


    [System.Serializable]
    public class btn
    {
        public GameObject mainButton;       //button for activating other tower buttons
        public GameObject towerButtons;     //the parent of all tower buttons
        public GameObject powerUpButtons;   //the parent of all battle power up buttons
		
        public GameObject button_sell;      //button to sell the selected tower
        public GameObject button_upgrade;   //button to upgrade the selected tower
        public GameObject button_abort;     //button to disable the upgrade menu

        public GameObject button_showPassive; //button to show the passive powerup panel
        public GameObject button_buyPassive; //button to buy the passive powerup selection
		
        public GameObject button_exit;      //button to leave self-control mode
        public GameObject mobile_shoot;     //button to shoot on mobile devices
    }


    [System.Serializable]
    public class lbl
    {
        public Text headerName;      //header label for the power up / tower name
        public Text properties;      //label for displaying power up / tower properties
                                        //(shared for tooltip/upgrade menu)
        public Text stats;           //label for current level tower stats
        public Text upgradeInfo;     //label for visualing stats on the next tower level
        public Text[] price;         //initial price of the tower
        public Text[] sellPrice;     //value at which the tower will be sold

        public Text passivePUName;   //label for displaying the selected passive powerup name
        public Text passivePUDescription; //label for the selected passive powerup description
        public Text[] passivePUPrice;  //labels to display the passive powerup prices
    }


    [System.Serializable]
    public class snd
    {
        public AudioClip build;     //sound to play on placing a bought tower
        public AudioClip sell;      //sound to play on selling a selected tower
        public AudioClip upgrade;   //sound to play on upgrading a selected tower
    }


    //SELF CONTROL VARIABLES
    [System.Serializable]
    public class con
    {
        public GameObject crosshair;        //mouse position crosshair prefab while controlling a tower
        //prefab showing an animated line between the self controlled tower and our mouse position
        public GameObject aimIndicator;
        //extra height for our camera, which will be added on selfcontrol
        //- to "sit" on top of a tower while controlling it
        public float towerHeight = 10f;
        public Slider relSlider;          //reloading progress bar
        public UnityEngine.UI.Image relSprite;    //foreground bar sprite for changing its color
    }
}