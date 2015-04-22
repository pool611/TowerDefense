/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//this script handles the tower AI
public class TowerBase : MonoBehaviour
{
    //projectile prefab to instantiate
    public GameObject projectile;
    //shot position transform
    public Transform shotPos;
    //sound to play on shot
    public AudioClip shotSound;
    //effect to instantiate on shot
    public GameObject shotFx;
    //animation clips for idle and shot
    public AnimationClip idleAnim;
    public AnimationClip shotAnim;
    //range indicator prefab
    public GameObject rangeInd;

    //our rotating tower part
    public Transform turret;
    //field of view
    public float shotAngle = 0f;
    //delay variable for next shot
    [HideInInspector]
    public float lastShot = 0f;

    //enemies whose are in range of this tower
    [HideInInspector]
    public List<GameObject> inRange = new List<GameObject>();
     
    //all possible enemy types
    public enum EnemyType
    {
        Ground,
        Air,
        Both,
    }
    //this towers attackable enemy types
    public EnemyType myTargets = EnemyType.Both;

    //all possible auto-fire modes
    public enum ShootOrder
    {
        FirstIn,    //first seen
        LastIn,     //last seen
        Strongest,  //highest health
        Weakest //lowest health
    }
    //default auto-fire mode
    public ShootOrder shootOrder = ShootOrder.FirstIn;
    //current target selected by auto-fire mode
    [HideInInspector]
    public Transform currentTarget;

    //damage multiplier if self control is active, 1 = 100% = no additional damage
    public float controlMultiplier = 1;
    //instantiation space between projectiles in self control mode
    public float projSpacing = 3;
    //upgrade script reference for current shoot delay timers and damage
    [HideInInspector]
    public Upgrade upgrade;

    //reference to the original powerup that is currently active
    private DefensivePowerUp powerup;
    //partial copy of the original powerup used for clean up
    private DefensivePowerUp tempPowerup;
    //partial copy of upgrade stats, storing the differences between powerups
    private List<UpgOptions> tempOptions;


    void Awake()
    {
        //disable on awake, otherwise this tower would start
        //shooting at enemies when we still place it onto grids
        this.enabled = false;
    }


    void Start()
    {
        //zero out rotation on shot position transform,
        //otherwise the field of view gets reversed
        if (shotPos.localRotation != Quaternion.identity)
            shotPos.localRotation = Quaternion.identity;

        //set tower script of rotating tower part if we have a turret
        if (turret)
        {
            //a turret with a shotAngle of zero can't work properly
            if(shotAngle == 0)
                Debug.LogWarning("Turret set on '" + gameObject.name + "' but shot angle equals zero!");

            TowerRotation turScript = turret.gameObject.GetComponent<TowerRotation>();

            if (turScript)
                turScript.towerScript = this;
            else
                Debug.LogWarning("Turret set on '" + gameObject.name + "' but no TowerRotation script attached to it!");
        }

        //start tower AI
        StartInvoke(0f);
    }


    //tower AI main method, gets called at Start() and also by RangeTrigger.cs when
    //an enemy enters our tower range. Disabled if we control this tower
    public void StartInvoke(float delay)
    {
        //if this script is enabled - the tower is placed and active
        //and if CheckRange() isn't running already, check our tower area every 'shootDelay'-seconds
        if (this.enabled == true && !IsInvoking("CheckRange") && 
           (!SV.control || SV.control.gameObject != gameObject))
        {
            //we keep repeating this check at every delay interval
            //seems like a unity bug: the first parameter (delay) can't be zero,
            //this will result in a check after 1 second instead 0 - as a workaround we add 0.1 seconds
            InvokeRepeating("CheckRange", delay + 0.1f, upgrade.options[upgrade.curLvl].shootDelay);
        }
    }


    //check tower radius area
    void CheckRange()
    {
        //we have no enemy in range, cancel repeating calls
        if (inRange.Count == 0)
        {
            //cancel InvokeRepeating() of StartInvoke()
            CancelInvoke("CheckRange");
            //clear target (so TowerRotation.cs skips turret rotation)
            currentTarget = null;
            //play idle animation, if set
            if (idleAnim) animation.Play(idleAnim.name);
            //don't execute further code
            return;
        }

        //there are targets, instantiate automatic projectiles
        AutoAttack();
    }


    //auto shot
    void AutoAttack()
    {
        //set current shoot time to delay next one
        lastShot = Time.time;

        //get current and sorted list of enemies which are in range
        //this list is also filtered with the maximum projectile amount
        List<Transform> targets = GetTargets();
        //check if we had potential enemies
        if (targets.Count == 0)
        {
            //no valid targets found, try again in 0.5 seconds
            CancelInvoke("CheckRange");
            InvokeRepeating("CheckRange", 0.5f, upgrade.options[upgrade.curLvl].shootDelay);
            return;
        }

        //shoot at every target of that enemy list
        for (int i = 0; i < targets.Count; i++)
            InstantiateProjectile(targets[i]);

        //try to instantiate shot particle effect
        ShotEffect();
    }


    //method to return a sorted list of enemies in range
    //filtered by auto-fire mode and field of view
    public List<Transform> GetTargets()
    {
        //cache maximum possible amount of projectiles/enemies
        int targetCount = upgrade.options[upgrade.curLvl].targetCount;
        //initialize enemy list
        List<Transform> targets = new List<Transform>();
        if (inRange.Count <= 0) return targets;

        //distinguish between auto-fire modes
        switch (shootOrder)
        {
            //first seen
            case ShootOrder.FirstIn:
                //get first enemy in range
                currentTarget = inRange[0].transform;
                
                //loop through enemies in range and
                //add the enemy to our final list if possible
                for (int i = 0; i < inRange.Count; i++)
                {
                    //we already have the maximum amount of enemies in our list
                    //compared with amount of projectiles
                    if (targets.Count == targetCount)
                        break;

                    //cache current enemy transform in list
                    Transform enemy = inRange[i].transform;

                    //if field of view (towerRotate) is deactivated,
                    //or if the current enemy is within our field of view,
                    //it is a potential target and we add it to the final enemy list
                    if (!turret || (turret && CheckAngle(enemy)))
                        targets.Add(enemy);
                }
                break;

            //last seen
            case ShootOrder.LastIn:
                //get last enemy in range
                currentTarget = inRange[inRange.Count - 1].transform;

                //loop through enemies in range and
                //add the enemy to our final list if possible
                for (int i = 0; i < inRange.Count; i++)
                {
                    //we already have the maximum amount of enemies in our list
                    //compared with amount of projectiles
                    if (targets.Count == targetCount)
                        break;

                    //cache current last enemy transform in list - started from the end
                    Transform enemy = inRange[inRange.Count - i - 1].transform;

                    //if field of view (towerRotate) is deactivated,
                    //or if the current enemy is within our field of view,
                    //it is a potential target and we add it to the final enemy list
                    if (!turret || (turret && CheckAngle(enemy)))
                        targets.Add(enemy);
                }
                break;

            //highest or lowest health
            //some parts of these fire modes are the same, we divide them later again 
            case ShootOrder.Strongest:
            case ShootOrder.Weakest:

                //initialize list of enemy properties
                List<Properties> allProperties = new List<Properties>();

                //loop through all enemies which are in range
                //and get their Properties component
                for (int i = 0; i < inRange.Count; i++)
                {
                    allProperties.Add(inRange[i].GetComponent<Properties>());
                }

                //this is the different part of both modes
                switch (shootOrder)
                {
                    //highest health
                    case ShootOrder.Strongest:
                        //loop through all enemies to sort them
                        //based on their health points
                        for (int i = 0; i < inRange.Count; i++)
                        {
                            //initialize temporary health value
                            //(a possible enemy should have more than zero health)
                            float highestHealth = 0;
                            //strongest enemy of this list
                            Transform strongest = null;
                            //temporary list index value
                            int index = 0;

                            //loop through all properties and find the highest health
                            for (int j = 0; j < allProperties.Count; j++)
                            {
                                //get current enemy health
                                float enemyHealth = allProperties[j].health;
                                //compare it to the health found before
                                //if it is higher, set it as strongest enemy to date
                                if (enemyHealth > highestHealth)
                                {
                                    //set this enemy as strongest enemy
                                    strongest = allProperties[j].transform;
                                    //set this health to highest health
                                    highestHealth = enemyHealth;
                                    //remember enemy index out of this loop
                                    index = j;
                                }
                            }

                            //add strongest enemy found in the current list
                            //to the final list and remove it for the next iteration
                            targets.Add(strongest);
                            allProperties.RemoveAt(index);
                        }
                        break;
                    //lowest health
                    case ShootOrder.Weakest:
                        //loop through all enemies to sort them
                        //based on their health points
                        for (int i = 0; i < inRange.Count; i++)
                        {
                            //initialize temporary health value
                            //(devised value, a possible enemy should have less than this)
                            float lowestHealth = float.MaxValue;
                            //weakest enemy of this list
                            Transform weakest = null;
                            //temporary list index value
                            int index = 0;

                            //loop through all properties and find the lowest health
                            for (int j = 0; j < allProperties.Count; j++)
                            {
                                //get current enemy health
                                float enemyHealth = allProperties[j].health;
                                //compare it to the health found before
                                //if it is lower, set it as weakest enemy to date
                                if (enemyHealth < lowestHealth)
                                {
                                    //set this enemy as lowest enemy
                                    weakest = allProperties[j].transform;
                                    //set this health to lowest health
                                    lowestHealth = enemyHealth;
                                    //remember enemy index out of this loop
                                    index = j;
                                }
                            }

                            //add weakest enemy found in the current list
                            //to the final list and remove it for the next iteration
                            targets.Add(weakest);
                            allProperties.RemoveAt(index);
                        }
                        break;
                }

                //set the first enemy (strongest or weakest) as the current target
                currentTarget = targets[0].transform;

                //if this tower has a turret,
                //filter the final list by field of fiew
                if (turret)
                {
                    //initialize temporary enemy list
                    //(we can't simultaneously search and remove the same key in the same list)
                    Transform[] temp = new Transform[targets.Count];
                    targets.CopyTo(temp);

                    //loop through all enemies and check if they're within field of view
                    foreach (Transform enemy in temp)
                    {
                        if (!CheckAngle(enemy))
                        {
                            //this enemy is apart, remove it from the final list
                            //Debug.Log("out of view: " + enemy.name);
                            targets.Remove(enemy);
                        }
                    }
                }

                //lastly we filter the enemy list by the projectile amount
                //and exclude the range of enemies above this value
                if (targets.Count > targetCount)
                {
                    targets.RemoveRange(targetCount, targets.Count - targetCount);
                }
                break;
        }

        //return final filtered enemy list by
        //auto-fire mode, field of view, projectile amount
        //Debug.Log(targets.Count);
        return targets;
    }


    bool CheckAngle(Transform enemy)
    {
        //calculate rotation angle to shoot (in 2D Space, x-z axis)
        //Vector2 preparations, get enemy x+z and shot pos x+z positions
        Vector2 targetVec = new Vector2(enemy.position.x, enemy.position.z);
        Vector2 shootVec = new Vector2(shotPos.position.x, shotPos.position.z);
        //the direction to the target
        Vector2 targetDir = targetVec - shootVec;
        //calculate angle with given vectors relative to shootPos
        Vector2 forward = new Vector2(shotPos.forward.x, shotPos.forward.z);
        float angle = Vector2.Angle(forward, targetDir);

        //check whether enemy is within this shot angle
        //'angle' determines the angle value from forward direction to our enemy,
        //'shotAngle' determines the complete angle, so we need shotAngle/2
        if (angle <= shotAngle / 2)
        {
            //within angle
            return true;
        }
        else
        {
            //out of angle
            return false;
        }
    }


    void ShotEffect()
    {
        //play fire sound
        AudioManager.Play(shotSound, shotPos.position);

        //play shot animation, if set
        if (shotAnim)
            animation.Play(shotAnim.name);

        //instantiate particle effect, if set
        //parent to the shot transform for applying rotation
        if (shotFx)
        {
            Quaternion rot = Quaternion.identity;
            if (turret) rot = turret.rotation;
            GameObject fx = PoolManager.Pools["Particles"].Spawn(shotFx, shotPos.position, rot);
            fx.transform.parent = shotPos;
        }
    }


    //we control this tower, we have a clicked target position
    public void SelfAttack(Vector3 targetPos)
    {
        //cache current level projectile amount from Upgrade.cs for later use
        int targets = upgrade.options[upgrade.curLvl].targetCount;

        //check if possible target variable is equal to 1 or above and not divisible by 2: 1,3,5,7,9...
        if (targets >= 1 && targets % 2 != 0)
        {
            //fire one projectile to the targeted position (in the middle)
            //(this projectile flies straight to its target)
            InstantiateProjectile(targetPos);
        }

        //get direction from tower to crosshair position
        Vector3 direction = (targetPos - transform.position).normalized;
        //lay it out horizontally: rotate the direction by 90 degrees,
        //so we have a left and right direction at the target position
        direction = Quaternion.AngleAxis(-90, Vector3.up) * direction;
        //zero out y axis, so the direction does not affect the projectiles height
        direction.y = 0f;

        //we can have more than one target
        if (targets > 1)
        {
            //fire half of projectiles to the left, with some spacing between them
            for (int i = 0; i < targets / 2; i++)
            {
                InstantiateProjectile(targetPos + (-direction * projSpacing * (i + 1)));
            }
            //fire half of projectiles to the right, with some spacing between them
            for (int i = 0; i < targets / 2; i++)
            {
                InstantiateProjectile(targetPos + (direction * projSpacing * (i + 1)));
            }
        }

        //try to instantiate shot particle effect 
        ShotEffect();
    }


    //this function instantiates the projectile and sets its target
    //case 1: target is set, this is an automatic projectile, set its enemy target.
    void InstantiateProjectile(Transform target)
    {
        //spawn/activate projectile instance
        GameObject ProjectileInst = PoolManager.Pools["Projectiles"].Spawn(projectile, shotPos.position, shotPos.rotation);
        //get Projectile component
        Projectile proj = ProjectileInst.GetComponent<Projectile>();

        //set damage to current level damage
        proj.damage = upgrade.options[upgrade.curLvl].damage;

        //set target of projectile, so it can follow it
        proj.target = target;
    }


    //this function instantiates the projectile and sets its target
    //case 2: target position "targetPos" is set, we control the tower, set projectile's target position.
    void InstantiateProjectile(Vector3 targetPos)
    {
        //spawn/activate projectile instance
        GameObject ProjectileInst = PoolManager.Pools["Projectiles"].Spawn(projectile, shotPos.position, shotPos.rotation);
        //get Projectile component
        Projectile proj = ProjectileInst.GetComponent<Projectile>();

        //set damage to current level damage
        proj.damage = upgrade.options[upgrade.curLvl].damage;

        //enable self control mode
        proj.followEnemy = false;
        //set projectile's target position
        proj.endPos = targetPos;
        //increase damage by muliplier factor
        proj.damage = proj.damage * controlMultiplier;
    }


    //activate power up on this tower
    //called by GUI -> PowerUps.cs
    public void ApplyPowerUp(DefensivePowerUp powerup)
    {
        //do not allow multiple powerups at the same time,
        //remove previous powerup differences
        if (this.powerup != null)
        {
            StopCoroutine("PowerUpBoost");
            RemovePowerUp();
        }

        //cache powerup for later remove
        //and start coroutine for boosting this tower
        this.powerup = powerup;
        StartCoroutine("PowerUpBoost");
    }


    IEnumerator PowerUpBoost()
    {
        //Debug.Log("buff started: " + Time.time);

        //buff TowerBase variables
        shotAngle *= powerup.buff.shotAngle;
        controlMultiplier *= powerup.buff.controlMultiplier;
        //create new powerup instance for cleaning up later
        tempPowerup = new DefensivePowerUp();
        tempOptions = new List<UpgOptions>();
        //iterate over upgrade settings and buff every option in each level
        //(even if the user upgrades a tower, this will still ensure buffed values)
        for (int i = 0; i < upgrade.options.Count; i++)
        {
            UpgOptions opt = upgrade.options[i];
            tempOptions.Add(new UpgOptions());
            tempOptions[i].radius = opt.radius;
            tempOptions[i].damage = opt.damage;
            tempOptions[i].shootDelay = opt.shootDelay;
            tempOptions[i].targetCount = opt.targetCount;
            //first cache buff difference, then set new values
            //when the buff ends we substract the difference again
            tempOptions[i].radius -= opt.radius *= powerup.buff.radius;
            tempOptions[i].damage -= opt.damage *= powerup.buff.damage;
            tempOptions[i].shootDelay -= opt.shootDelay /= powerup.buff.shootDelay;
            tempOptions[i].targetCount -= opt.targetCount += powerup.buff.targetCount;
        }

        //if an effect is set, spawn buff fx and
        //cache the particle gameobject reference in our temporary instance 
        if (powerup.buffFx)
            tempPowerup.buffFx = PoolManager.Pools["Particles"].Spawn(powerup.buffFx, transform.position, Quaternion.identity);

        //update tower range collider/indicator
        upgrade.RangeChange();

        //restart shot timer with new values (shootDelay)
        CancelInvoke("CheckRange");       
        float newDelay = upgrade.options[upgrade.curLvl].shootDelay;
        //calculate time when this tower can shoot again
        float remainTime = lastShot + newDelay - Time.time;
        if (remainTime < 0) remainTime = 0;
        //and take the minimum value as new invoke delay
        if (remainTime > newDelay)
            StartInvoke(newDelay);
        else
            StartInvoke(remainTime);

        //wait until the powerup is over before removing it
        yield return new WaitForSeconds(powerup.duration);
        RemovePowerUp();

        //Debug.Log("buff ended: " + Time.time);
    }


    //remove the current powerup, undo all the changes
    //see the previous method PowerUpBoost() for comments
    void RemovePowerUp()
    {
        shotAngle /= powerup.buff.shotAngle;
        controlMultiplier /= powerup.buff.controlMultiplier;

        for (int i = 0; i < upgrade.options.Count; i++)
        {
            UpgOptions opt = upgrade.options[i];
            opt.radius += tempOptions[i].radius;
            opt.damage += tempOptions[i].damage;
            opt.shootDelay += tempOptions[i].shootDelay;
            opt.targetCount += tempOptions[i].targetCount;
        }

        //unspawn buff fx if we instantiated one
        if (tempPowerup.buffFx)
            PoolManager.Pools["Particles"].Despawn(tempPowerup.buffFx);

        upgrade.RangeChange();

        //restart shot timer, again
        CancelInvoke("CheckRange");
        float newDelay = upgrade.options[upgrade.curLvl].shootDelay;
        float remainTime = lastShot + newDelay - Time.time;
        if (remainTime < 0) remainTime = 0;

        if (remainTime > newDelay)
            StartInvoke(newDelay);
        else
            StartInvoke(remainTime);
        //unset powerup references
        powerup = null;
        tempPowerup = null;
    }


    //draw visual editor lines for each enemy in range
    //draw field of view
    void OnDrawGizmosSelected()
    {
        //switch color to red, so all following lines will be red
        Gizmos.color = Color.red;

        //do not draw any gizmos if this script is disabled
        //(for example in the editor or ingame while placing a floating tower)
        if (this.enabled == false)
            return;

        //draw a line from the tower to each enemy which is in range
        if (inRange.Count > 0)
        {
            foreach (GameObject enemy in inRange)
            {
                Gizmos.DrawLine(shotPos.position, enemy.transform.position);
            }
        }
		
		//do not draw angle gizmos if this tower has no turret
        if (!turret)
       	return;

        //switch color to green, so all following lines will be green
		Gizmos.color = Color.green;

        //visualize field of view ( 3 seperate rays for straight, forward right and forward left lines )
        //cache current tower radius and the half angle (for the left/right side)
        float currentRadius = upgrade.options[upgrade.curLvl].radius;
        float halfShootAngle = shotAngle / 2;

        //forward
        //draw a ray with forward direction and current given radius
        //transform local direction vector to world space
        Vector3 forwarddir = turret.TransformDirection(Vector3.forward) * currentRadius;
        Gizmos.DrawRay(turret.position, forwarddir);

        //forward right
        //get quaternion angle to multiply by direction
        Quaternion locRight = Quaternion.AngleAxis(halfShootAngle, Vector3.up);
        //( local direction vector points in forward direction but also 'halfShootAngle' right )
        Vector3 rightDir = turret.TransformDirection(locRight * Vector3.forward) * currentRadius;
        Gizmos.DrawRay(turret.position, rightDir);

        //forward left
        //get quaternion angle to multiply by direction
        Quaternion locLeft = Quaternion.AngleAxis(halfShootAngle, Vector3.down);
        //( local direction vector points in forward direction but also 'halfShootAngle' left )
        Vector3 leftDir = turret.TransformDirection(locLeft * Vector3.forward) * currentRadius;
        Gizmos.DrawRay(turret.position, leftDir);
    }

    
    //no floating tower and no upgrade menu is shown, also we are not in self control mode
    //so we can show this tower's range indicator on mouse enter action
    void OnMouseEnter()
    {
        if (!SV.selection && !SV.showUpgrade && !SV.control && !SV.showExit)
        rangeInd.renderer.enabled = true;
    }


    //disable range indicator if mouse left this gameobject and none of these options are turned on
    void OnMouseExit()
    {
        if (!SV.selection && !SV.showUpgrade && !SV.control && !SV.showExit)
        rangeInd.renderer.enabled = false;
    }    
}
