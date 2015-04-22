/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

//Enemy Properties and Damage/Slow/Death Methods
public class Properties : MonoBehaviour
{
    public float health = 100;        //health points
    [HideInInspector]
    public float maxhealth;          //variable for auto caching start health
    public Slider healthbar;        //3D health indicator slider
    public Shield shield;       //absorb shield properties
    private RectTransform barParentTrans; //bar rect transform

    public GameObject hitEffect;    //particle effect to show if enemy gets hit
    public GameObject deathEffect;  //particle effect to show on enemy death
    public AudioClip hitSound;   //sound to play on hit via AudioManager
    public AudioClip deathSound; //sound to play on death via AudioManager
    //animation component
    [HideInInspector]
    public Animation anim;
    public AnimationClip walkAnim;  //animation to play during walk time
    public AnimationClip dieAnim;  //animation to play on death
	public AnimationClip successAnim; //animation to play on end of path
	private Vector3 animPos; //relative animation position at the start

    public float[] pointsToEarn;  //points we get if we kill this enemy, array to support multiple resources
    public int damageToDeal = 1;    //game health we lose if this enemy reaches its destination
    [HideInInspector]
	public TweenMove myMove; //store movement component of this object for having access to movement
    //list to cache references for all towers whose range we entered
    [HideInInspector]
    public List<TowerBase> nearTowers = new List<TowerBase>();
    //time value to delay instantiation/activation of particle effects
    //(we do not want to spawn a new effect on every hit as this drastically decreases performance)
    private float time;


    void Start()
    {
        //add reference of this script to PoolManager's Properties dictionary,
        //this gets accessed by Projectile.cs on hit so we don't need a GetComponent call every time
        PoolManager.Props.Add(gameObject.name, this);

        //get movement reference
        myMove = gameObject.GetComponent<TweenMove>();
        //get animation component
        anim = gameObject.GetComponentInChildren<Animation>();
		animPos = anim.transform.localPosition;

        //initialize our maxSpeed value once, at game start.
        //we don't need and want to set that on every spawn ( OnSpawn() )
        //because the last enemy could have altered it at runtime (via slow)
        myMove.maxSpeed = myMove.speed;
        //also get this gameobject ID once, at runtime it stays the same
        myMove.pMapProperties.myID = gameObject.GetInstanceID();

        //get healthbar/shield slider transform for rotating it later
        if (healthbar)
            barParentTrans = healthbar.transform.parent.GetComponent<RectTransform>();
        else if (shield.bar)
            barParentTrans = shield.bar.transform.parent.GetComponent<RectTransform>();

        //apply passive powerups to this enemy
        PowerUpManager.ApplyToSingleEnemy(this, myMove);
    }


    //on every spawn, play walk animation if one is set via the inspector
    IEnumerator OnSpawn()
    {
        //wait the first frame until Start() was executed
        yield return new WaitForEndOfFrame();

        if (walkAnim)
        {
            //randomize playback offset and play animation
            anim[walkAnim.name].time = Random.Range(0f, anim[walkAnim.name].length);
            anim.Play(walkAnim.name);
        }

        //workaround for Unity uGUI bug resetting the rect position
        if (barParentTrans) 
        {
            RectTransform[] rects = GetComponentsInChildren<RectTransform>();
            for (int i = 0; i < rects.Length; i++)
                rects[i].anchoredPosition = Vector2.zero;
        }

        //store start health and absorb value for later use (resetted on Despawn())
        maxhealth = health;
        shield.maxValue = shield.value;
    }


    //reposition/center hud bar so it always looks at our game camera
    void LateUpdate()
    {
        Quaternion rot = Camera.main.transform.rotation;

        if (barParentTrans)
            barParentTrans.rotation = rot;
    }


    //we entered the range of a tower, add it to our nearTowers dictionary
    public void AddTower(TowerBase tower)
    {
        nearTowers.Add(tower);
    }


    //remove towers which are too far away
    //called by RangeTrigger.cs on OnTriggerExit()
    public void RemoveTower(TowerBase tower)
    {
        nearTowers.Remove(tower);
    }


    //we got hit by a projectile
    public void Hit(float damage)
    {
        //check if our health points are greater than zero and we aren't dying, else return
        if (!IsAlive()) return;

        //if we have a shield, first do damage on the shield
        //then start its regeneration
        if (shield.enabled)
        {
            damage = HitShield(damage);
            StopCoroutine("RegenerateShield");
            StartCoroutine("RegenerateShield");
        }

        //do the remaining damage (after the shield, if set)
        //directly to the health points
        health -= damage;

        //check whether we survived this hit. Only call OnHit()
        //if it wasn't called within the last 2 seconds (reason defined above)
        if (health > 0 && Time.time > time + 2)
            OnHit();
        else if (health <= 0)
        {
            //loop through pointsToEarn and add all points to our resources array for this enemy kill
            for (int i = 0; i < pointsToEarn.Length; i++)
                GameHandler.SetResources(i, pointsToEarn[i]);
            //track the correct amount of enemies alive
            GameHandler.EnemyWasKilled();
            //remove enemy
            OnDeath();
        }

        //adjust 3d unit bars
        SetUnitFrame();
    }

	
	public void SetUnitFrame()
    {
        //calculate life/shield value in percentage
        //and set that as slider value
        if (healthbar)
            healthbar.value = health / maxhealth;
        if (shield.bar)
            shield.bar.value = shield.value / shield.maxValue;
    }


    //absorbs damage and returns remaining damage value
    float HitShield(float damage)
    {
        float currentValue = shield.value;
        //do damage to the shield
        shield.value -= damage;
        if (shield.value < 0) shield.value = 0;
        //calculate remaining damage
        damage -= currentValue;
        if (damage < 0) damage = 0;
        //return new damage value
        return damage;
    }


    //regenerate shield value and take options into account
    IEnumerator RegenerateShield()
    {
        //without regeneration setting, break immediately
        if (shield.regenValue <= 0) yield break;
        //wait regeneration delay defined in the shield settings
        yield return new WaitForSeconds(shield.delay);
        
        //regenerate shield value to the max over time
        while (shield.value < shield.maxValue)
        {
            //regenerate with a fixed value, simply add it
            if (shield.regenType == TDValue.fix)
                shield.value += shield.regenValue;
            else
                //calculate percentage regeneration based on maxValue and add it to the shield
                shield.value += Mathf.Round(shield.maxValue * shield.regenValue * 100f) / 100f;
            //do not exceed the max shield value
            shield.value = Mathf.Clamp(shield.value, 0f, shield.maxValue);
            //set new shield bar slider value
            shield.bar.value = shield.value / shield.maxValue;
            //wait delay before the next regeneration step continues
            yield return new WaitForSeconds(shield.interval);
        }
    }
	

    //play hit sound and instantiate hit particle effect
    void OnHit()
    {
        //set current time value,
        //so this method doesn't get called the next 2 seconds anymore
        time = Time.time;

        //play hit sound via AudioManager
        //add some random pitch so it sounds differently on every hit 
        AudioManager.Play(hitSound, transform.position, Random.Range(1.0f - .2f, 1.0f + .1f));

        //instantiate hit effect at object's position if one is set
        if (hitEffect)
            PoolManager.Pools["Particles"].Spawn(hitEffect, transform.position, hitEffect.transform.rotation);
    }


    //OnDeath() stops movement, removes us from other towers, grants money for this kill,
    //plays a death sound and instantiates a particle effect before despawn
    void OnDeath()
    {
        //stop all running tweens (through TweenMove) and coroutines
        myMove.StopAllCoroutines();
        myMove.CancelInvoke("Accelerate");
        myMove.tween.Kill();
        StopAllCoroutines();

        //say to all towers that this enemy died/escaped
        //despawn this enemy
        StartCoroutine("RemoveEnemy");
    }

	
	//checks whether this enemy is still alive
    //takes health, gameobject state, animations as well as movement into account
    public bool IsAlive()
    {
        if (health <= 0 || !gameObject.activeInHierarchy || (dieAnim && anim.IsPlaying(dieAnim.name))
            || myMove.tween == null || myMove.tween.isComplete)
            return false;
        else
            return true;
    }

	
    //slow method - called by Projectile.cs
    //passed arguments are time and slow factor
    public void Slow(float slowTime, float slowFactor)
    {
        //don't apply slow if this enemy died already or was despawned
        if (health <= 0 || gameObject.activeInHierarchy == false
            || (dieAnim && anim.IsPlaying(dieAnim.name)))
            return;

        //calculate new speed value with slow
        float newSpeed = myMove.maxSpeed * slowFactor;

        //the projectile that hit this object wants to slow it down -
        //we need to check if our current speed is faster than the slowed down speed,
        //so this impact would skip weaker slows
        if (myMove.speed >= newSpeed)
        {
            //set new speed with slow applied 
            myMove.speed = newSpeed;
            myMove.Slow();

            //stop (existing?) acceleration
            myMove.CancelInvoke("Accelerate");
            //start accelerating again in slowTime-seconds
            myMove.Invoke("Accelerate", slowTime);
        }
    }


    //damage over time method - called by Projectile.cs
    //passed arguments are damage, time and frequency
    public IEnumerator DamageOverTime(float[] vars)
    {
        //we don't need any health checks here, because Projectile.cs
        //checks that already - to not start a new coroutine at all

        /*
            short example what we would like to see:
            vars[0] = total damage over time = 5
            vars[1] = time = 6
            vars[2] = frequency = 4
         
            delay = time between damage calls = 2 seconds
            dotDmg = damage per call = 1.25
            
            result:
            1st damage at 0 seconds = 1.25
            2nd damage at 2 seconds = 1.25
            3rd damage at 4 seconds = 1.25
            4th damage at 6 seconds = 1.25
            ------------------------------
            total damage: 5 over 6 seconds
        */

        //calculate interval of damage calls
        //we directly apply the first damage on impact,
        //therefore we reduce one frequency before
        float delay = vars[1] / (vars[2]-1);
        //calculate resulting damage per call
        float dotDmg = vars[0] / vars[2];

        //instantly apply first damage
        Hit(dotDmg);

        //yield through all further damage calls based on frequency
        for (int i = 0; i < vars[2]-1; i++)
        {
            //wait interval for next hit
            yield return new WaitForSeconds(delay);
            //apply next damage
            Hit(dotDmg);
        }

        //search particle gameobject which was parented to this burning enemy 
        foreach (Transform child in transform)
        {
            //we use a general "(Clone)" search because spawned particle effects
            //contain this string and there really shouldn't be any other clone parented
            if (child.name.Contains("(Clone)"))
                PoolManager.Pools["Particles"].Despawn(child.gameObject);
        }
    }


    //we reached our destination, called by TweenMove.cs
    void PathEnd()
    {
        //we couldn't stop this enemy, do damage to our game health points
        GameHandler.DealDamage(damageToDeal);
        //remove enemy
        OnDeath();
    }


    //before this enemy gets despawned (on death or in case it reached its destination),
    //say to all towers that this enemy died
    IEnumerator RemoveEnemy()
    {
        //remove ourselves from each tower's inRange list
        for (int i = 0; i < nearTowers.Count; i++)
            nearTowers[i].inRange.Remove(gameObject);

        //clear our inRange list
        nearTowers.Clear();

        //remove possible 'damage over time' particle effects (see DamageOverTime())
        foreach (Transform child in transform)
        {
            if (child.name.Contains("(Clone)"))
                PoolManager.Pools["Particles"].Despawn(child.gameObject);
        }

        //if this object used the Progress Map (in TweenMove.cs)
        if (myMove.pMapProperties.enabled)
        {
            //stop calculating our ProgressMap path progress
            myMove.CancelInvoke("ProgressCalc");

            //call method to remove it from the map 
            ProgressMap.RemoveFromMap(myMove.pMapProperties.myID);
        }

        //this enemy died, handle death stuff
        if (health <= 0)
        {
            //if set, instantiate deathEffect at current position
            if (deathEffect)
                PoolManager.Pools["Particles"].Spawn(deathEffect, transform.position, Quaternion.identity);

            //play sound on death via AudioManager
            AudioManager.Play(deathSound, transform.position);

            //handle death animation
            //wait defined death animation delay if set
            if (dieAnim)
            {
                anim.Play(dieAnim.name);
                yield return new WaitForSeconds(dieAnim.length);
            }
        }
        //the enemy didn't die, and escaped instead 
        else
        {
            if (successAnim)
            {
                anim.Play(successAnim.name);
                yield return new WaitForSeconds(successAnim.length);
            }
        }
		anim.transform.localPosition = animPos;

        //reset all initialized variables for later reuse
        //set start health and shield value back to maximum health/absorb
        health = maxhealth;
        shield.value = shield.maxValue;
        //reset 3D bars to indicate 100% health/shield points
        if (healthbar)
            healthbar.value = 1;
        if (shield.bar)
            shield.bar.value = 1;

        //despawn/disable us
        PoolManager.Pools["Enemies"].Despawn(gameObject);
    }
	
	
	//remove us from the list for all properties scripts if
    //this enemy object gets destroyed. We have to do this
    //so a new enemy with the same name can take its spot
    void OnDestroy()
    {
        PoolManager.Props.Remove(gameObject.name);
    }
}


[System.Serializable]
public class Shield
{
    public bool enabled = false;        //if the object should use a shield
    public Slider bar;                //3d shield bar indicator
    public float value = 10;            //absorb value
    [HideInInspector]
    public float maxValue;              //variable for caching start absorb value
    public TDValue regenType = TDValue.fix; //fixed or percentual regeneration
    public float regenValue = 0;             //regeneration value for each step
    public float interval = 1;          //interval in seconds between shield regeneration steps
    public float delay = 1;             //delay before regeneration starts
}