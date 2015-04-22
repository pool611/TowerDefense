/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

//example GUI for the advanced scene
//implementation of the GUILogic.cs script with customized behavior
//basically the same as GUIImpl.cs, but in this gui implementation we don't use:
//-extra button to show/hide the tower buttons (method ShowButtons() and button 'mainButton' removed)
//-all mobile related code
//and added:
//-player healthbar, set in SetHealthbar()
//-self control reloading animation with sprites instead of a slider, see DrawReload()
public class Adv_GUIImpl : MonoBehaviour
{
    //reference to the main logic
    private GUILogic gui;

    //PARTICLE FX
    public GameObject buildFx;
    public GameObject upgradeFx;

    //healthbar slider
    public Slider healthSlider;

    //GUI ELEMENTS
    public pan panels = new pan();
    public btn buttons = new btn();
    public spr sprites = new spr();
    public lbl labels = new lbl();
    public snd sound = new snd();
    public con control = new con();

    //time value to insert a delay between mouse clicks
    //(we do not want to show the upgrade menu instantly after a tower was bought)
    private float time;


    //get reference of the main GUI script
    void Awake()
    {
        gui = GetComponent<GUILogic>();
    }


    void Start()
    {
        //instantiate self control mouse position indicator at a non visible position
        //so SelfControl.cs later has access to it and can change its position when we right click on a tower to control it
        control.crosshair = (GameObject)Instantiate(control.crosshair, SV.outOfView, Quaternion.identity);
        //the same with AimPrefab, it indicates the flight path of projectiles of a tower
        control.aimIndicator = (GameObject)Instantiate(control.aimIndicator, SV.outOfView, Quaternion.identity);
    }


    //fade out tooltip and upgrade panel and deactivate all current selections
    public void DisableMenus()
    {
        //toggle upgrade menu visibility value
        SV.showUpgrade = false;
        CancelInvoke("UpdateUpgradeMenu");

        //fade out tooltip and upgrade panel
        gui.StartCoroutine("FadeOut", panels.tooltip);
        gui.StartCoroutine("FadeOut", panels.upgradeMenu);
        //destroy current selections and free variables
        gui.CancelSelection(true);

        //disable range indicator visibility if set
        if (gui.towerBase) gui.towerBase.rangeInd.renderer.enabled = false;
    }


    //update is called every frame,
    //here we check if the user pressed the escape button (desktop or mobile)
    //and recalculate player's healthbar values
    void Update()
    {
        CheckESC();
        SetHealthbar();

        //one by one: if our mouse is not over a UI element and the exit menu is not shown
        //then: if we performed a click and do not control a tower,
        //and our mouse is not over a tower OR the tower is not the selected one
        //and our mouse is not over a grid OR the we clicked on a grid while the upgrade menu is active
        if (EventSystem.current.IsPointerOverGameObject() || SV.showExit)
            return;
        else if (Input.GetMouseButtonUp(0) && !SV.control
            && (!gui.currentTower || SV.selection && gui.currentTower.transform.parent.gameObject != SV.selection)
            && (!gui.currentGrid || SV.showUpgrade && gui.currentGrid))
        {
            //disable all menus if we have no floating tower, aren't controlling
            //any tower and clicked somewhere in the game when not over gui elements
            DisableMenus();
        }

        //react to GUILogic.cs raycasts
        ProcessGrid();
        ProcessTower();
    }


    //recalculates player health values and sets them for the health slider/bar
    void SetHealthbar()
    {
        healthSlider.value = GameHandler.gameHealth / GameHandler.maxHealth;
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
            //deactivate all tower buttons
            buttons.towerButtons.SetActive(false);
            //disable active selections
            DisableMenus();
            //toggle exit menu visibility variable
            SV.showExit = true;
            //pause the game
            Time.timeScale = 0.0001f;
            //hide fast-forward button
            buttons.button_speed.SetActive(false);
        }
    }


    void ProcessGrid()
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

            //we bought a tower by pressing the left mouse button while over the grid
            if (Input.GetMouseButtonUp(0))
            {
                //--> purchase successful
                BuyTower();
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
        if (Input.GetMouseButtonUp(0) && time + 0.5f < Time.time && !SV.control)
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
        if (Input.GetMouseButtonUp(1) && time + 0.5f > Time.time)
        {
            //attach self control script to this tower
            EnableSelfControl();
        }
    }


    //instantiate floating tower when a tower button was clicked
	public void CreateTower(int index)
    {
        //the upgrade menu is shown, disable this menu and the range indicator
        if (SV.showUpgrade && gui.towerBase)
        {
            gui.towerBase.rangeInd.renderer.enabled = false;
            SV.showUpgrade = false;
            CancelInvoke("UpdateUpgradeMenu");
        }

        //indicating our TowerManager tower index
        //(first button = 0 = first tower, second button = second tower in the list etc.)
        //try to instantiate floating tower
        gui.InstantiateTower(index);

        //show tool tip menu
        ShowTooltipMenu(index);

        //if a tower was created, toggle range indicator visibility on
        if(SV.selection)
            gui.towerBase.rangeInd.renderer.enabled = true;
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

        //disable previous range indicator if one is set already
        //(old one from another tower)
        if (gui.towerBase)
            gui.towerBase.rangeInd.renderer.enabled = false;

        //set tower properties
        gui.SetTowerComponents(tower);
        //show tower range indicator
        gui.towerBase.rangeInd.renderer.enabled = true;

        //refresh values instantly
        UpdateUpgradeMenu();
        //periodically refresh the upgrade tooltip with current values 
        if (!IsInvoking("UpdateUpgradeMenu"))
            InvokeRepeating("UpdateUpgradeMenu", 0f, 1f);
    }


    void UpdateUpgradeMenu()
    {
        //store current upgrade level
        int curLvl = gui.upgrade.curLvl;
        //store necessary upgrade option info for later use
        UpgOptions upgOptions = gui.upgrade.options[curLvl];

        //set UI label properties with information of this tower
        labels.towerName.text = gui.upgrade.gameObject.name;
        labels.properties.text = "Level:" + "\n" +
                                "Radius:" + "\n" +
                                "Damage:" + "\n" +
                                "Delay:" + "\n" +
                                "Targets:";
        //visualize current tower stats
        labels.stats.text = +curLvl + "\n" +
                                upgOptions.radius + "\n" +
                                upgOptions.damage + "\n" +
                                upgOptions.shootDelay + "\n" +
                                upgOptions.targetCount;

        //visualize tower stats on the next level if the label was set
        if (labels.upgradeInfo)
        {
            //check if we have a level to upgrade left
            if (curLvl < gui.upgrade.options.Count - 1)
            {
                //get upgrade option for the next level and display stats
                UpgOptions nextUpg = gui.upgrade.options[curLvl + 1];
                labels.upgradeInfo.text = "= " + (curLvl + 1) + "\n" +
                                          "= " + nextUpg.radius + "\n" +
                                          "= " + nextUpg.damage + "\n" +
                                          "= " + nextUpg.shootDelay + "\n" +
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
            labels.sellPrice[i].text = sellPrice[i].ToString();

            //check if we can buy another upgrade level
            //if not, erase price values
            if (!gui.AvailableUpgrade())
            {
                affordable = false;
                labels.price[i].text = "";
                continue;
            }

            //set price label for upgrading to the next level
            labels.price[i].text = upgradePrice[i].ToString();
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
        if(upgradeFx)
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

        //store tower base properties from TowerManager lists
        //(in case we haven't instantiated a tower because it
        //wasn't affordable, we can't access an instance and
        //have to use these components pre-stored in TowerManager.cs)
        TowerBase baseOptions = gui.towerScript.towerBase[index];
        //store necessary upgrade option info for later use
        UpgOptions upgOptions = gui.towerScript.towerUpgrade[index].options[0];

        //set all information related to tower properties,
        //such as tower name, properties and initial price
        labels.towerName.text = gui.towerScript.towerNames[index];
        labels.properties.text = "Projectile:" + "\n" +
                                "Radius:" + "\n" +
                                "Damage:" + "\n" +
                                "Delay:" + "\n" +
                                "Targets:";
        //visualize current tower stats
        labels.stats.text = baseOptions.projectile.name + "\n" +
                                upgOptions.radius + "\n" +
                                upgOptions.damage + "\n" +
                                upgOptions.shootDelay + "\n" +
                                baseOptions.myTargets;

        //set visible label price text for each resource
        for (int i = 0; i < GameHandler.resources.Length; i++)
            labels.price[i].text = upgOptions.cost[i].ToString();
    }


    //game exit menu
    public void ExitMenu(int index)
    {
        //toggle exit menu visibility
        SV.showExit = false;
        //resume game with normal speed
        Time.timeScale = 1;

        //no matter what we clicked (main menu or cancel),
        //we hide the exit menu again and enable tower buttons
        gui.StartCoroutine("FadeOut", panels.main);
        buttons.towerButtons.SetActive(true);
        //also enable fast-forward button again
        buttons.button_speed.SetActive(true);
        ChangeGameSpeed(null); //reinitialize button image

        //handle exit button
        if (index == 1)
        {
            //load our first scene - the main menu
            Application.LoadLevel("Intro");
        }
    }


    //speed up/slow down the game
    public void ChangeGameSpeed(GameObject button)
    {
        //get corresponding button image component from gameobject called "Sprite"
		UnityEngine.UI.Image image = buttons.button_speed.transform.FindChild("Sprite")
			.GetComponent<UnityEngine.UI.Image>();

        //when this method gets called with empty parameters,
        //only reset button image to default and return
        if (button == null)
        {
            image.sprite = sprites.fastForward;
            return;
        }

        //fast forward or slow down game speed based on current timescale
        //and switch images to the opposite one
        if (Time.timeScale <= 1)
        {
            Time.timeScale = 2;
            image.sprite = sprites.playForward;
        }
        else
        {
            Time.timeScale = 1;
            image.sprite = sprites.fastForward;
        }
    }


    //attach self control component to the selected tower
    public void EnableSelfControl()
    {
        //we right-clicked/double tapped the tower we control already,
        //or the main menu is shown, do nothing (return)
        if (SV.control || SV.showExit)
            return;

        //deactivate all tower buttons
        //while controlling a tower we don't want them to be visible
        buttons.towerButtons.SetActive(false);

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
        //initialize self control variables - skip mobile parameter
        SV.control.Initialize(gameObject, control.crosshair, control.aimIndicator,
                              control.towerHeight, false);
        //update components to controlled tower
        gui.SetTowerComponents(gui.currentTower);
        //check remaining reload time for this tower
        StartCoroutine("DrawReload");

        //enable exit tower button
        panels.control.SetActive(true);
    }


    //terminate interactive tower control
    public void DisableSelfControl()
    {
        //remove control mechanism    
        SV.control.Terminate();

        //stop iterating through the reloading texture
        StopCoroutine("DrawReload");

        //set tower buttons back to active
		panels.control.SetActive(false);
        buttons.towerButtons.SetActive(true);
    }


    //animate frames of reload texture. Gets started by SelfControl.cs
    //in Attack(), if the controlled tower attacked
    public IEnumerator DrawReload()
    {
        //divide delay / total frames so we get waiting time after each frame
        //in order to complete it within the given delay time
        //cache delay for shooting at this level
        float shootDelay = gui.upgrade.options[gui.upgrade.curLvl].shootDelay;
        control.frameDuration = shootDelay / control.frames;
        //per default, start animating the reload texture from the first frame
        int startFrame = 1;

        //calculate time when this tower can shoot again
        float remainTime = gui.towerBase.lastShot + shootDelay - Time.time;

        //cache sprite
        UnityEngine.UI.Image img = control.image;

        //remainTime > 0 means this tower already shot within the last few seconds,
        //so we have to calculate the right frame based on the remaining time
        if (remainTime > 0)
        {
            //calculate how long it does take until the next shot and start from
            //that reload texture frame. skip if remainTime is equal to shootDelay,
            //since startFrame is already set to 1 per default
            if(remainTime < shootDelay)
                startFrame = (int)(control.frames - (remainTime / shootDelay) * 10);
        }
        else
        {
            //there is no need to animate the slider,
            //the tower is not reloading, set texture frame back to zero
			img.sprite = sprites.reloading[0];
            yield break;
        }

        //begin from frame "startFrame" and count through all further frames,
        //so this sets the current visible frame ( animating texture )
        for (int curF = startFrame; curF < control.frames; curF++)
        {
            //set sprite according to reload time and index
			img.sprite = sprites.reloading[curF];

            //delay further execution with frame time,
            //so the whole animation plays within one shot delay of the tower
            yield return new WaitForSeconds(control.frameDuration);
        }
        //if loop ended, set frame index back to zero
		img.sprite = sprites.reloading[0];
    }


    //PANEL, BUTTON, LABEL elements
    //SOUND effects and SELF CONTROL variables

    [System.Serializable]
    public class pan
    {
        public GameObject main;         //exit menu panel
        public GameObject upgradeMenu;  //upgrade menu panel
        public GameObject tooltip;      //tooltip menu panel
		public GameObject control;		//self control panel
    }


    [System.Serializable]
    public class btn
    {
        public GameObject towerButtons;     //the parent of all tower buttons

        public GameObject button_sell;      //button to sell the selected tower
        public GameObject button_upgrade;   //button to upgrade the selected tower
        public GameObject button_abort;     //button to disable the upgrade menu

        public GameObject button_exit;      //button to leave self-control mode

        public GameObject button_speed;     //button to speed up/slow down the game
    }


    [System.Serializable]
    public class spr
    {
        public Sprite playForward;
        public Sprite fastForward;
		public Sprite[] reloading;
    }


    [System.Serializable]
    public class lbl
    {
        public Text towerName;       //label for the tower name
        public Text properties;      //label for the tower properties (shared for tooltip/upgrade menu)
        public Text stats;           //label for current level tower stats
        public Text upgradeInfo;     //label for visualing stats on the next tower level
        public Text[] price;         //initial price of the tower
        public Text[] sellPrice;     //value at which the tower will be sold
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
        public GameObject crosshair;    //mouse position crosshair prefab while controlling a tower
        //prefab showing an animated line between the self controlled tower and our mouse position
        public GameObject aimIndicator;
        //extra height for our camera, which will be added on selfcontrol
        //- to "sit" on top of a tower while controlling it
        public float towerHeight = 10f;
        public UnityEngine.UI.Image image;             //texture/sprite shown during reloading
        public int frames;              //texture frames in total
        [HideInInspector]
        public float frameDuration;     //length of one reload
    }
}