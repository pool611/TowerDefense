/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//class for all possible projectile configurations
public class Projectile : MonoBehaviour
{
    //target to fly to or to follow (null on tower self control)
    [HideInInspector]
    public Transform target; 
    //damage to deal, this value is set from the corresponding tower
    [HideInInspector]
    public float damage = 0f;
    
    //if this projectile should follow the target
    //(modified to false by TowerBase.cs at InstantiateProjectile() on tower control)
    public bool followEnemy = true;   
    //cache followEnemy for resetting it on despawn     
    private bool cacheFollowEnemy;

    //trajectory configuration
    public enum FireMode
    {
        straight,
        lob,
    }
    //default trajectory chosen in inspector
    public FireMode flyMode = FireMode.straight;
    //how high the lob arc should be
    public float lobHeight = 0f;
    //how fast the projectile flies
    public float speed = 0f;
  
    //perform additional collision detection, should be enabled for fast moving projectiles
    //Unity skips colliders if the projectile moves that fast, so it "teleports" behind the enemy
    //credits to Daniel Brauer & Adrian for their "DontGoThroughThings" script on the Unity wiki:
    //http://wiki.unity3d.com/index.php/DontGoThroughThings
    public bool continuousCollision = false;
    //variables for continuous collision detection,
    //mostly collider sizes and rigidbody positions
    private float minimumBounds;
    private float sqrMinimumBounds;
    private Vector3 previousPosition;
    private Rigidbody myRigidbody;

    //sound to play on impact
    public AudioClip impactSound;

    //whether this projectile should deal damage to the enemy that was hit or not
    //(e.g. in case it's just an area effect)
    public bool singleDMG = true;

    //time before despawning after the projectile hit something
    public float timeToLast = 0;

    //display each possible projectile property in the inspector
    public Explosion explosion;
    public Burn burn;
    public Slow slow;

    //start (instantiate) position, needed for lob calculation
    [HideInInspector]
    public Vector3 startPos = Vector3.zero;
    //fly with constant time factor, needed for lob calc.
    private float time = 0f;

    //final target position to fly to (directly set by TowerBase.cs on tower control)
    [HideInInspector]
    public Vector3 endPos = Vector3.zero;
    //if this projectile hit an enemy - trigger variable to only execute an impact once
    private bool executed;


    //initialize variables for continuous collision detection
    void Init()
    {
        myRigidbody = rigidbody;
        minimumBounds = collider.bounds.size.z;
        sqrMinimumBounds = minimumBounds * minimumBounds;
    }


    //initialize freshly spawned projectile instance
    IEnumerator OnSpawn()
    {
        //if not set before, init first time
        if (!myRigidbody) Init();
		//reset impact trigger
        executed = false;

        //we save the start position of this transform for being able to:
        //-check if this projectile goes further than our tower range (then we disable it)
        //-and calculate lob curve based on origin (we need this in flyMode == FireMode.lob)
        startPos = transform.position;
        //also initialize starting position for collision detection
        previousPosition = myRigidbody.position;
        //reset lob timer
        time = 0f;
        //cache follow setting
        cacheFollowEnemy = followEnemy;

        //OnSpawn() gets called simultaneously with the instantiation by TowerBase.cs,
        //but TowerBase.cs also sets some properties of this script and we have to wait after that
        //here we wait one frame until all necessary variables are initialized
        yield return new WaitForEndOfFrame();

        //if we are not following the enemy,
        //the end position is equal to the current enemy position
        if (target && !followEnemy)
            endPos = target.position;

        //rotate projectile to enemy direction
        transform.LookAt(endPos);

        //start the right looping coroutine depending on flyMode
        switch (flyMode)
        {
            case FireMode.straight:
                while (!executed)
                    yield return StartCoroutine("Straight");
                break;
            case FireMode.lob:
                while (!executed)
                    yield return StartCoroutine("Lob");
                break;
        }
    }


    //straight trajectory
    void Straight()
    {
        //if this projectile should follow an enemy which is active
        if (followEnemy && target && target.gameObject.activeInHierarchy)
        {
            //constantly update end position and look at enemy
            endPos = target.position;
            transform.LookAt(endPos);
        }

        //move this gameobject forward with given speed (in the direction to 'endPos')
        transform.Translate(Vector3.forward * (speed * 10) * Time.deltaTime * Time.timeScale);

        //if enabled, perform additional collision detection
        //don't continue to not check the flown distance if we hit something here
        if (continuousCollision)
            if(ContinuousCollision()) return;

        //if still active - measure flown distance, remove projectile after leaving tower radius
        //this could happen if the projectile flies to a target in the air
        if (Vector3.Distance(transform.position, startPos) > Vector3.Distance(startPos, endPos) + 0.5f)
        {
            //execute impact
            StartCoroutine("HandleImpact");
        }
    }


    //lob trajectory
    void Lob()
    {
        //if this projectile should follow an enemy which is active
        //constantly update end position
        //else set it to null (target is inactive, lost reference)
        if (followEnemy && target && target.gameObject.activeInHierarchy)
            endPos = target.position;
        else if (target)
            target = null;

        // calculate current time within our lerping time range
        time += 0.2f * Time.deltaTime;
        //adjust time with speed
        float cTime = time * speed;     

        // calculate straight-line lerp position:
        Vector3 currentPos = Vector3.Lerp(startPos, endPos, cTime);

        // add a value to Y, using Sine to give a curved trajectory in the Y direction
        currentPos.y += lobHeight * Mathf.Sin(Mathf.Clamp01(cTime) * Mathf.PI);

        //smoothly look at our computed Y position (flight path, not directly at our target )
        //if currentPos is equal to transform.position that returns Vector3.zero and logs an error,
        //therefore we first need to check this case here
        if (currentPos != transform.position)
        {
            //calculate rotation and adjust it to the flight path
            Quaternion rotation = Quaternion.LookRotation(currentPos - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 6.0f);
        }

        //finally assign the computed position to our gameObject
        transform.position = currentPos;

        //if enabled, perform additional collision detection,
        //don't continue to not check the target distance if we hit something here
        if (continuousCollision)
            if (ContinuousCollision()) return;

        //if still active - measure if we get near our target/end position
        if (Vector3.Distance(transform.position, endPos) < 0.1f)
        {
            //execute impact
            StartCoroutine("HandleImpact");
        }
    }


    //collision detection for fast moving objects,
    //based on raycasts between the previous and current position
    //returns true if we hit something in between these positions
    bool ContinuousCollision()
    {
        //calculate moved distance between the last and current position 
        Vector3 movementThisStep = myRigidbody.position - previousPosition;
        float movementSqrMagnitude = movementThisStep.sqrMagnitude;

        //if we have moved more than our collider size,
        //so we could have missed a collision
        if (movementSqrMagnitude > sqrMinimumBounds)
        {
            //calculate moved distance for the current frame
            //and raycast against all colliders in this distance
            float movementMagnitude = Mathf.Sqrt(movementSqrMagnitude);
            RaycastHit[] hitInfos = Physics.RaycastAll(previousPosition, movementThisStep, movementMagnitude, SV.enemyMask);
            //iterate over all colliders that were hit and trigger the collision method
            for(int i = 0; i < hitInfos.Length; i++)
                StartCoroutine("OnTriggerEnter", hitInfos[i].collider);
            //set the current position as previous position
            previousPosition = myRigidbody.position;
            //return true if we hit something
            if (hitInfos.Length > 0) return true;
        }

        //set the current position as previous position
        //we haven't hit something, return false
        previousPosition = myRigidbody.position;
        return false;
    }


    //collision function
    IEnumerator OnTriggerEnter(Collider hitCol)
    {
        //only continue if still active
        if (executed) yield break;

        //get object which was hit for later comparison
        GameObject colGO = hitCol.gameObject;

        //check if the collided object is an enemy
        //and we should deal single dmg to it
        if (colGO.layer == SV.enemyLayer)
        {
            //we have no target, that means we control the tower
            //set collided object as target
            if (!target)
                target = colGO.transform;
            //a target is set - this is an automatic attack, check if the target
            //is equal to the collided target, else abort. (here we avoid
            //collisions when the projectile flies through several enemies)
            else if (followEnemy && target != colGO.transform)
                yield break;

            if (singleDMG)
            {
                //get enemy properties
                Properties targetProp = PoolManager.Props[colGO.name];
                //deal damage to the enemy
                targetProp.Hit(damage);
            }
        }
        else if (colGO.layer != SV.worldLayer)
            //this projectile hasn't even collided with the world,
            //abort. (theoretically this case shouldn't happen)
            yield break;

        //yield till impact execution is over
        yield return StartCoroutine("HandleImpact");        
    }


    //impact mediator
    IEnumerator HandleImpact()
    {
        //toggle boolean so this method won't execute twice
        executed = true;

        AudioManager.Play(impactSound, transform.position);

        //handle explosion
        if (explosion.enabled)
        {
            yield return StartCoroutine("Explosion");
        }

        //handle damage over time
        if (burn.enabled)
        {
            yield return StartCoroutine("Burn");
        }

        //handle slow
        if (slow.enabled)
        {
            yield return StartCoroutine("Slow");
        }

        //don't despawn before timeToLast is over
        if (timeToLast > 0)
        {
            float timer = Time.time + timeToLast;
            while (Time.time < timer)
            {
               if (target && PoolManager.Props[target.name].health <= 0)
                    break;

               yield return null;
            }
        }

        //once all projectile properties were fully executed, despawn this object
        PoolManager.Pools["Projectiles"].Despawn(gameObject);
    }


    void Explosion()
    {
        //if an explosion effect is set,
        //activate/instantiate it at the current gameobject position
        if (explosion.fx)
            PoolManager.Pools["Particles"].Spawn(explosion.fx, transform.position, Quaternion.identity);

        //when our projectile explodes, it should just hurt layer 'SV.enemyMask' (8 = Enemies)
        //check enemies in range (within the radius) and store their colliders
        List<Collider> cols = new List<Collider>(Physics.OverlapSphere(transform.position, explosion.radius, SV.enemyMask));

        //foreach enemy which was hit, substract defined health
        foreach (Collider col in cols)
        {
            //access stored properties of PoolManager
            Properties targetProp = PoolManager.Props[col.name];
            //apply damage based on explosion factor
            targetProp.Hit(damage * explosion.factor);
        }
    }


    void Burn()
    {
        //initialize array of values required by DamageOverTime() of Properties.cs
        //DamageOverTime() is a coroutine which applies damage over several seconds,
        //but we can only submit one parameter to a coroutine.
        //Because all needed values are floats, we simply use a float array 
        float[] vars = new float[3];
        //get total damage over time based on tower damage and multiplier factor
        vars[0] = damage * burn.factor;
        //get total time to apply all damage within
        vars[1] = burn.time;
        //get frequency of damage calls
        vars[2] = burn.frequency;

        //initialize list of targets
        List<Collider> cols = new List<Collider>();

        if (burn.area)
            //if our projectile deals area of effect, it should just hurt layer 'SV.enemyMask' (8 = Enemies)
            //check enemies in range (within the radius) and store their colliders
            cols = new List<Collider>(Physics.OverlapSphere(transform.position, burn.radius, SV.enemyMask));
        else if (target)
            //without area of effect, we only store the current target collider
            cols.Add(target.collider);

        //loop through colliders
        foreach (Collider col in cols)
        {
            //cache transform of collider
            Transform colTrans = col.transform;

            //get enemy properties from the PoolManager dictionary
            Properties targetProp = PoolManager.Props[colTrans.name];

            //skip this enemy if it died already
            if (!targetProp.IsAlive())
                continue;

            //stop (existing?) damage over time coroutine
            if(!burn.stack)
                targetProp.StopCoroutine("DamageOverTime");
            //start new DoT coroutine of Properties.cs with given parameters
            targetProp.StartCoroutine("DamageOverTime", vars);

            //damage over time particle effect
            //if no particle effect is set for this method,
            //skip further code and continue to next iteration
            if (!burn.fx) continue;

            //initialize effect transform
            Transform dotEffect = null;
            //loop through children and find existing particle effect
            foreach (Transform child in colTrans)
            {
                if (child.name.Contains(burn.fx.name))
                {
                    //we found the effect parented to the target,
                    //set it here
                    dotEffect = child;
                }
            }

            //if no parented effect was found in the loop above,
            //we create a new one
            if (!dotEffect)
            {
                //activate/instantiate new particle effect via PoolManager at target's position
                dotEffect = PoolManager.Pools["Particles"].Spawn(burn.fx, colTrans.position, Quaternion.identity).transform;
                //attach effect to the target so it moves with it
                dotEffect.parent = colTrans;
            }
        }
    }


    void Slow()
    {
        //if an slow effect is set,
        //activate/instantiate it at the current gameobject position
        if (slow.fx)
            PoolManager.Pools["Particles"].Spawn(slow.fx, transform.position, Quaternion.identity);

        //initialize list of targets
        List<Collider> cols = new List<Collider>();

        if (slow.area)
            //if our projectile deals area of effect, it should just hurt layer 'SV.enemyMask' (8 = Enemies)
            //check enemies in range (within the radius) and store their colliders
            cols = new List<Collider>(Physics.OverlapSphere(transform.position, slow.radius, SV.enemyMask));
        else if (target)
            //without area of effect, we only store the current target collider
            cols.Add(target.collider);

        //loop through colliders
        foreach (Collider col in cols)
        {
            //get enemy properties from the PoolManager dictionary
            Properties targetProp = PoolManager.Props[col.name];
            //call Slow() within Properties.cs with given parameters
            targetProp.Slow(slow.time, slow.factor);
        }
    }


    //called on impact
    //reset all initialized variables for later use
    void OnDespawn()
    {
        target = null;
        followEnemy = cacheFollowEnemy;
    }
}


[System.Serializable]
public class Explosion
{
    //should this projectile deal area damage
    public bool enabled;
    //FX on impact
    public GameObject fx;
    //area damage radius
    public float radius;
    //how much additional damage in % of tower damage this option deals
    public float factor;  
}


[System.Serializable]
public class Slow
{
    //whether this projectile slows enemies
    public bool enabled;
    //FX on impact
    public GameObject fx;
    //slow down an area around the impact?
    public bool area;
    //area radius
    public float radius;
    //time for slowing enemies down
    public float time;
    //how much in % this projectile slows enemies down
    public float factor;   
}


[System.Serializable]
public class Burn
{
    //whether this projectile deals damage over time
    public bool enabled;
    //FX on impact - attached to the enemy
    public GameObject fx;
    //deal damage over time to all enemies around?
    public bool area;
    //area impact radius
    public float radius;
    //total time to apply the full damage
    public float time;
    //delay between damage calls
    public int frequency;
    //how much additional damage in % of tower damage this option deals
    public float factor;
    //whether or not damage over time can stack
    public bool stack;
}


//currently used for powerups only,
//pasted in here as the other damage classes are here as well
[System.Serializable]
public class Weakening
{
    //whether the powerup should weaken the enemy 
    public bool enabled;
    //FX on impact
    public GameObject fx;
    //weaken enemies in an area
    public bool area;
    //area radius
    public float radius;
    //type to deal damage to
    public enum Type
    {
        healthOnly,
        shieldOnly,
        all
    }
    //variable that represents the type
    public Type type = Type.all;
    //how much damage in % of powerup damage this option deals
    public float factor;
}