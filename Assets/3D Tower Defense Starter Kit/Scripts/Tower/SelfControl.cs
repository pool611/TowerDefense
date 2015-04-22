/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

//interactive tower control
public class SelfControl : MonoBehaviour
{
    //main camera to animate to tower
    private Transform cam;
    //main camera control script to disable unnecessary user input
    private CameraControl camDesktop;
    //controlled tower base script
    private TowerBase towerScript;
    //controlled tower turret script
    private TowerRotation towerRotScript;
    //current tower level 
    private int curLvl;
    //next calculated shot time in game time
    private float shotTime;
    //attackable targets
    private TowerBase.EnemyType enemyType;

    //target position indicator transform
    private Transform cross;
    //LineRenderer component of aimIndicator prefab
    private LineRenderer aimRend;

    //GUI gameobject for attack callbacks
    private GameObject guiObj;

    //mobile
    private bool centerCrosshair = false;


    //initialize all needed variables,
    //and let the camera fly to the current tower position
    public void Initialize(GameObject guiObj, GameObject crosshair, GameObject aimIndicator,
                           float towerHeight, bool mobile)
    {
        //get scene main camera
        cam = Camera.main.transform;
        //set GUI gameobject passed in
        this.guiObj = guiObj;

        //recognized this application as non-mobile
        //set camera script desktop version
        if (!mobile)
        {
            //get camera control script of main camera
            camDesktop = cam.GetComponent<CameraControl>();
            //disable control script, so all standard movement is disabled
            //(we only want to rotate around while controlling our tower)
            camDesktop.enabled = false;
        }

        //get TowerBase.cs of tower we control (this gameobject)
        towerScript = gameObject.GetComponent<TowerBase>();
        //enable range indicator so it is always visible
        towerScript.rangeInd.renderer.enabled = true;
        //disable tower intelligence, since we want to self control this tower
        towerScript.CancelInvoke("CheckRange");

        //set current tower upgrade level through TowerBase.cs -> Upgrade.cs
        //(we cache them, so we don't need to call this every time in the functions below)
        curLvl = towerScript.upgrade.curLvl;
        //calculate next shot time in the future: last shot game time + delay
        shotTime = towerScript.lastShot + towerScript.upgrade.options[curLvl].shootDelay;
        //also cache attackable enemy type
        enemyType = towerScript.myTargets;

        //TowerRotate of TowerBase.cs is set: we have a turret that rotates
        if (towerScript.turret)
        {
            //get TowerRotation component and disable it:
            //our turret does not follow nearby enemies anymore
            //(we want our turret to follow our mouse/touch input.)
            towerRotScript = towerScript.turret.GetComponent<TowerRotation>();
            towerRotScript.enabled = false;
        }

        //set crosshair transform reference, since GUILogic.cs instantiated this prefab already
        cross = crosshair.transform;
        //set crosshair positioning behaviour
        centerCrosshair = mobile;

        //get LineRenderer component of the prefab instantiated by GameGUI.cs
        aimRend = aimIndicator.GetComponent<LineRenderer>();
        //set start and end position of LineRenderer
        //[0] = start position, [1] = end position, at first we initialize both to towers shoot position
        aimRend.SetPosition(0, towerScript.shotPos.position);
        aimRend.SetPosition(1, towerScript.shotPos.position);

        //move the camera to our controlled tower position
        Holoville.HOTween.HOTween.To(cam, 1f, "position", transform.position + new Vector3(0, towerHeight, 0), false, Holoville.HOTween.EaseType.EaseInOutExpo, 0f);
        //we don't want to move our crosshair until iTween's animation is completed and the camera reached its destination
        //StartCoroutine() doesn't support delay - and Invoke() doesn't support starting a coroutine
        //so we need to invoke another function in 1 second which then starts the coroutine
        Invoke("StartCoroutineUpdate", 1);
    }

    
    //Invoked above, only used to start the main coroutine in this script below
    void StartCoroutineUpdate()
    {
        StartCoroutine("CoroutineUpdate");
    }
    

    //runs every frame
    //check mouse input and reposition crosshair
    IEnumerator CoroutineUpdate()
    {
        //loop
        while (true)
        {
            //playing on non-mobile device
            if (camDesktop)
            {
                //check for rotation input
                camDesktop.Rotate();

                //trigger attack on mouse click, but not over gui elements
                if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
                {
                    Attack();
                }
            }

            //place crosshair at mouse/touch position
            PositionCrosshair();

            yield return null;
        }
    }


    void PositionCrosshair()
    {
        //don't raycast against worldMask if our input is over the gui
        //or the main menu is shown, so don't reposition the crosshair over it
        //IsPointerOverEventSystemObject does not work on mobiles. Instead, on mobile
        //devices we center the crosshair in the middle of the screen.
        //this surpasses a gui detection and does not require additional touches.
        if (EventSystem.current.IsPointerOverGameObject()
            && !centerCrosshair || SV.showExit)
            return;

        //declare crosshair detection ray
        Ray ray;
        //variable for ray hit parameters
        RaycastHit rayHit;

        if (centerCrosshair)
        {
            //center crosshair in the middle of the screen on mobile devices
            ray = new Ray(cam.position, cam.forward);
        }
        else
        {
            //on pc/mac, cast a ray from screen position to our mouse input in 3d position
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        }

        //cast ray against worldMask (Layer "WorldLimit"), so if it has hit something:
        if (Physics.Raycast(ray, out rayHit, 300, SV.worldMask))
        {
            //draw debug line from ray origin to hit position
            //Debug.DrawLine(ray.origin, rayHit.point);

            //if our tower has a turret
            if (towerRotScript)
            {
                //rotate turret to look at our crosshair position
                //but exclude y axis so it does not rotate up- and downwards
                towerScript.turret.LookAt(new Vector3(cross.position.x, towerScript.turret.position.y, cross.position.z));
            }

            //calculate distance between tower position and hit point
            //so we can check if mouse position is within tower radius in the step below 
            float dist = Vector3.Distance(transform.position, rayHit.point);
            //get current tower radius on every method call,
            //this value could update because of a powerup
            float radius = towerScript.upgrade.options[curLvl].radius;

            //distance is within radius and has hit the terrain ground
            //(place crosshair at ground) --- ground attack mode
            if (dist <= radius && rayHit.transform.tag == "Ground")
            {
                //position crosshair little above hit point so it does not go into terrain
                cross.position = rayHit.point + new Vector3(0, 0.2f, 0);
                //clear crosshair rotation, it should look straight upwards to the sky
                cross.localEulerAngles = Vector3.zero;
                //position LineRenderer start pos to (rotating/moving) shoot position
                //and end pos to hit point / crosshair position
                aimRend.SetPosition(0, towerScript.shotPos.position);
                aimRend.SetPosition(1, cross.position);
            }
            //we have not hit the ground and our tower can attack air targets
            //so we switch to --- air attack mode
            else if (enemyType == TowerBase.EnemyType.Air ||
                     enemyType == TowerBase.EnemyType.Both)
            {
                //distance from crosshair to hit point is lower than radius in air attack mode:
                //this occurs if there are some obstacles right in front of the tower
                if (dist < radius)
                    //set crosshair position right before hit point / obstacle position
                    cross.position = rayHit.point - ray.direction;
                else
                    //limit crosshair position (based on mouse input) at max radius position (in the air)
                    cross.position = ray.origin + ray.direction * (radius + 2);

                //the crosshair should always look at us, so it is a flat plane
                cross.LookAt(cam);
                //we need to adjust the crosshair rotation by 90 degrees to face us,
                //the standard rotation of the model is rotated to a wrong direction
                cross.localEulerAngles += new Vector3(90, 0, 0);

                //again, adjust LineRenderer start and end position
                aimRend.SetPosition(0, towerScript.shotPos.position);
                aimRend.SetPosition(1, cross.position);
            }
        }
    }


    //this method calls the attack function of TowerBase.cs with given parameters
    public void Attack()
    {
        //get current shot delay and new calculated next shot time as of now
        float delay = towerScript.upgrade.options[curLvl].shootDelay;
        float newShotTime = towerScript.lastShot + delay;
        //compare calculated shot time (stored) with new shot time (current)
        //they couldn't be equal because of a powerup used in between,
        //so we get the new shot time instead of the old one
        if (Time.time < shotTime && newShotTime > shotTime)
            shotTime = newShotTime;

        //don't do anything if we haven't reached
        //the game time necessary for shooting again
        if (Time.time < shotTime)
            return;

        //reset shoot timer ( delay )
        towerScript.lastShot = Time.time;
        //cache actual next shot time
        shotTime = towerScript.lastShot + delay;

        //shoot projectile at given position
        towerScript.SelfAttack(cross.position);
        //parallel execute GUI method DrawReload()
        guiObj.SendMessage("DrawReload", SendMessageOptions.DontRequireReceiver);
    }


    //remove self control actions
    public void Terminate()
    {
        //stop main coroutine "CoroutineUpdate()"
        this.StopAllCoroutines();

        //enable cam control
        if(camDesktop)
        camDesktop.enabled = true;

        //disable range indicator
        towerScript.rangeInd.renderer.enabled = false;

        //if controlled tower has a turret, activate that again too
        if (towerRotScript)
        {
            towerRotScript.enabled = true;
        }

        //unset static self control variable as we leave the tower
        SV.control = null;

        //initially set the first target, so that the turret starts moving instantly
        towerScript.GetTargets();

        //calculate remaining time when this tower can shoot again
        //so we start the tower AI not until the time comes
        float lastShot = towerScript.lastShot;
        float invokeInSec = towerScript.upgrade.options[curLvl].shootDelay + lastShot - Time.time;
        if (invokeInSec < 0) invokeInSec = 0f;
        towerScript.StartInvoke(invokeInSec);

        //set crosshair and lineRenderer to a non visible position
        //(it's not necessary to occupy the garbage collector every time)
        cross.position = new Vector3(0, -100, 0);
        aimRend.SetPosition(0, new Vector3(0, -100, 0));
        aimRend.SetPosition(1, new Vector3(0, -100, 0));
        //finally, after clean up, remove this script
        Destroy(this);
    }
}
