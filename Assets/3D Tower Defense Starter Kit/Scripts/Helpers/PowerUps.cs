/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


//main powerup class, that stores all general properties.
//other battle powerups are derived from this class
[System.Serializable]
public class BattlePowerUp
{
    public string name = "";        //powerup name to display
    public string description = ""; //description to display at runtime
    public bool enabled = true;     //whether this powerup is available
    public float startDelay;        //delay before applying activation methods
    public float cooldown;          //cooldown before a reuse is possible
    public Text timerText;       //label for displaying remaining cooldown time
    public int targets = 1;         //how many targets to affect
    public AudioClip sound;         //sound to play on activation
    public GameObject fx;           //effect to spawn on activation
    public GameObject indicator;    //area/target indicator

    [HideInInspector]
    public Transform target;        //clicked target transform
    [HideInInspector]
    public Vector3 position;        //clicked world position


    //instantiate particle fx and play sound, if set
    public void InstantiateFX()
    {
        if (fx)
            PoolManager.Pools["Particles"].Spawn(fx, position, Quaternion.identity);

        if (sound)
            AudioManager.Play(sound, position);
    }


    //sort target colliders based on distance
    public List<Collider> SortByDistance(List<Collider> cols)
    {
        //create new sorted list, but return if empty
        List<Collider> sortedCols = new List<Collider>();
        if (cols.Count == 0) return sortedCols;

        //create temporary list for modification purposes
        //populate with default values
        List<Collider> tempCols = new List<Collider>();
        for (int i = 0; i < cols.Count; i++)
            tempCols.Add(cols[i]);

        //iterate over all colliders
        for (int i = 0; i < cols.Count; i++)
        {
            //float value for storing the nearest distance,
            //appropriate collider variable and corresponding index
            float nearest = float.MaxValue;
            Collider col = null;
            int index = 0;
            //iterate over temporary list with colliders
            for (int j = 0; j < tempCols.Count; j++)
            {
                //skip this collider/enemy if it died already
                if (tempCols[j].gameObject.layer == SV.enemyLayer
                   && !PoolManager.Props[tempCols[j].gameObject.name].IsAlive())
                    continue;

                //calculate distance to the collider
                float distance = (tempCols[j].transform.position - position).sqrMagnitude;
                //if the distance is lower than the previous nearest distance,
                //and the enemy is still alive, set this collider as target,
                //overwrite nearest value and set index. redo for all colliders
                if (distance < nearest)
                {
                    col = tempCols[j];
                    nearest = distance;
                    index = j;
                }
            }

            //add collider to the sorted list, but don't add an empty collider
            //and remove it from the temporary list for another iteration
            if (col == null) break;
            sortedCols.Add(col);
            tempCols.RemoveAt(index);
        }

        //cap sorted list if count is greater than actual target amount
        if (sortedCols.Count > targets)
            sortedCols.RemoveRange(targets, sortedCols.Count - targets);
        //return sorted list
        return sortedCols;
    }


    //base methods, implemented in powerup classes
    public virtual bool CheckRequirements()
    { return false; }

    public virtual float GetMaxRadius()
    { return 0f; }
}


//offensive powerups, affecting enemies.
//derived from BattlePowerUp
[System.Serializable]
public class OffensivePowerUp : BattlePowerUp
{
    public TDValue damageType;                      //damage type (fix/percentual)
    public float damage;                            //damage value
    public bool singleTarget = false;               //toggle for single target only
    public Explosion explosion = new Explosion();   //explosion instance
    public Burn burn = new Burn();                  //burn instance
    public Slow slow = new Slow();                  //slow instance
    public Weakening weaken = new Weakening();      //weaken instance


    //do not continue if any of these requirements are not met:
    //the powerup is disabled (has cooldown),
    //a single target is set but the powerup has no target,
    //or we have both but the targeted enemy is not alive anymore
    public override bool CheckRequirements()
    {
        if (!base.enabled || singleTarget && !base.target
            || singleTarget && base.target && !PoolManager.Props[base.target.name].IsAlive())
        return false;
        else return true;
    }


    //same as in Projectile.cs, just with little modifications
    //to the trigger position and collider sorting based on distance
    //please refer to the projectile script for comments
    public IEnumerator Explosion()
    {
        List<Collider> cols = new List<Collider>(Physics.OverlapSphere(position, explosion.radius, SV.enemyMask));
        cols = SortByDistance(cols);

        for (int i = 0; i < cols.Count; i++)
        {
            Properties targetProp = PoolManager.Props[cols[i].name];
            if (damageType == TDValue.fix)
                targetProp.Hit(damage * explosion.factor);
            else
                targetProp.Hit(targetProp.maxhealth * damage * explosion.factor);

            if (explosion.fx)
                PoolManager.Pools["Particles"].Spawn(explosion.fx, cols[i].transform.position, Quaternion.identity);
        }

        yield return null;
    }


    //same as in Projectile.cs, just with little modifications
    //to the trigger position and collider sorting based on distance
    //please refer to the projectile script for comments
    public IEnumerator Burn()
    {
        List<Collider> cols = new List<Collider>();

        if (burn.area)
            cols = new List<Collider>(Physics.OverlapSphere(position, burn.radius, SV.enemyMask));
        else if (target)
            cols.Add(target.collider);

        cols = SortByDistance(cols);

        for (int i = 0; i < cols.Count; i++)
        {
            Transform colTrans = cols[i].transform;
            Properties targetProp = PoolManager.Props[colTrans.name];

            if (!targetProp.IsAlive())
                continue;

            float[] vars = new float[3];

            if (damageType == TDValue.fix)
                vars[0] = damage * burn.factor;
            else
                targetProp.Hit(targetProp.maxhealth * damage * burn.factor);
            vars[1] = burn.time;
            vars[2] = burn.frequency;

            if (!burn.stack)
                targetProp.StopCoroutine("DamageOverTime");
            targetProp.StartCoroutine("DamageOverTime", vars);

            if (!burn.fx) continue;

            Transform dotEffect = null;
            foreach (Transform child in colTrans)
            {
                if (child.name.Contains(burn.fx.name))
                    dotEffect = child;
            }

            if (!dotEffect)
            {
                dotEffect = PoolManager.Pools["Particles"].Spawn(burn.fx, colTrans.position, Quaternion.identity).transform;
                dotEffect.parent = colTrans;
            }
        }

        yield return null;
    }


    //same as in Projectile.cs, just with little modifications
    //to the trigger position and collider sorting based on distance
    //please refer to the projectile script for comments
    public IEnumerator Slow()
    {
        List<Collider> cols = new List<Collider>();

        if (slow.area)
            cols = new List<Collider>(Physics.OverlapSphere(position, slow.radius, SV.enemyMask));
        else if (target)
            cols.Add(target.collider);

        cols = SortByDistance(cols);

        for (int i = 0; i < cols.Count; i++)
        {
            Properties targetProp = PoolManager.Props[cols[i].name];
            targetProp.Slow(slow.time, slow.factor);

            if (slow.fx)
                PoolManager.Pools["Particles"].Spawn(slow.fx, cols[i].transform.position, Quaternion.identity);
        }

        yield return null;
    }


    //weakens the enemy by a percentual value
    //(percentual damage to health/shield or both)
    public IEnumerator Weaken()
    {
        //initialize list of targets
        List<Collider> cols = new List<Collider>();

        if (weaken.area)
            //if the powerup has an area of effect, it should just hurt layer 'SV.enemyMask' (8 = Enemies)
            //check enemies in range (within the radius) and store their colliders
            cols = new List<Collider>(Physics.OverlapSphere(position, weaken.radius, SV.enemyMask));
        else if (target)
            //without area of effect, we only store the clicked target collider
            cols.Add(target.collider);

        //sort colliders based on distance
        cols = SortByDistance(cols);

        //loop through colliders
        for (int i = 0; i < cols.Count; i++)
        {
            //if an weaken effect is set,
            //activate/instantiate it at the enemy's position
            if (weaken.fx)
                PoolManager.Pools["Particles"].Spawn(weaken.fx, cols[i].transform.position, Quaternion.identity);

            //get enemy properties from the PoolManager dictionary
            Properties targetProp = PoolManager.Props[cols[i].name];
            //cache health value
            float healthValue = targetProp.health;
            //initialize shield value and set it
            //if shield option of this enemy is enabled
            float shieldValue = 0;
            if (targetProp.shield.enabled)
                shieldValue = targetProp.shield.value;

            //differentiate between weaken type
            switch (weaken.type)
            {
                //only do damage to the shield value
                case Weakening.Type.shieldOnly:
                    if (shieldValue > 0)
                        targetProp.Hit(shieldValue * weaken.factor);
                    break;
                //only do damage to the health, ignoring shield value:
                //first we include the total shield value as damage value,
                //then reset it afterwards and update the unit bar frames
                case Weakening.Type.healthOnly:
                    targetProp.Hit(shieldValue + healthValue * weaken.factor);
                    targetProp.shield.value = shieldValue;
                    targetProp.SetUnitFrame();
                    break;
                //do damage to both the health and shield value simultaneously
                case Weakening.Type.all:
                    targetProp.Hit(shieldValue + healthValue * weaken.factor);
                    targetProp.shield.value = shieldValue * (1f - weaken.factor);
                    targetProp.SetUnitFrame();
                    break;
            }
        }

        yield return null;
    }


    //returns the maximum radius of all offensive attributes,
    //or zero on single target selection (no radius)
    public override float GetMaxRadius()
    {
        if (singleTarget) return 0f;
        
        float value = 0f;
        if (explosion.enabled && explosion.radius > value)
            value = explosion.radius;
        if (burn.enabled && burn.area && burn.radius > value)
            value = burn.radius;
        if (slow.enabled && slow.area && slow.radius > value)
            value = slow.radius;
        if (weaken.enabled && weaken.area && weaken.radius > value)
            value = weaken.radius;
        return value;
    }
}


//defensive powerups, affecting towers.
//derived from BattlePowerUp
[System.Serializable]
public class DefensivePowerUp : BattlePowerUp
{
    public GameObject buffFx;       //particle fx to instantiate at the tower position
    public float duration = 1;      //buff duration
    public bool area;               //whether this powerup affects an area
    public float radius;            //radius for area collision
    public Buff buff = new Buff();  //buff class instance


    //do not continue if any of these requirements are not met:
    //the powerup is disabled (has cooldown),
    //a single target is set but the powerup has no target
    public override bool CheckRequirements()
    {
        if (!base.enabled || !area && !base.target)
            return false;
        else
            return true;
    }


    //modifiable properties
    [System.Serializable]
    public class Buff
    {
        //TowerBase variables
        public float shotAngle = 1;
        public float controlMultiplier = 1;

        //Upgrade variables
        public float radius = 1;
        public float damage = 1;
        public float shootDelay = 1;
        public int targetCount = 0;
    }


    //on execution, this method boosts the towers triggered
    public IEnumerator BoostTowers()
    {
        //initialize list of targets
        List<Collider> cols = new List<Collider>();

        if (area)
            //area collision physics cast,
            //at the clicked position with defined radius against towers
            cols = new List<Collider>(Physics.OverlapSphere(position, radius, SV.towerMask));
        else if (target)
            //without area of effect, we only store the clicked tower target collider
            cols.Add(target.collider);

        //sort colliders based on distance
        cols = SortByDistance(cols);

        //loop through colliders and apply powerup to them
        for (int i = 0; i < cols.Count; i++)
        {
            TowerBase towerBase = cols[i].GetComponent<TowerBase>();
            towerBase.ApplyPowerUp(this);
        }

        yield return null;
    }


    //returns the area radius of defensive attributes,
    //or zero on non-area selections (no radius)
    public override float GetMaxRadius()
    {
        if (!area) return 0f;
        else return radius;
    }
}


//passive powerup class
[System.Serializable]
public class PassivePowerUp
{
    public string name = "";               //powerup name to display
    public string description = "";        //description to display at runtime
    public UnityEngine.UI.Image icon = null;             //icon sprite
    public Sprite defaultSprite = null;    //icon sprite name on unlock
    public Sprite enabledSprite = null;    //icon sprite name on purchase
	public UnityEngine.UI.Image onEnabledIcon = null;    //additional sprite to show on purchase
    public bool locked = false;            //whether this powerup is locked (requirements)
    public bool enabled = false;           //whether the user bought this powerup
    public float[] cost;                   //price for buying it

    //list of powerups required in order to unlock this one
    public List<int> req = new List<int>();

    //powerup properties (classes below)
    public PassiveTowerPowerUp towerProperties;
    public PassiveEnemyPowerUp enemyProperties;
    public PassivePlayerPowerUp playerProperties;

    //affected towers and/or enemies
    public List<string> selectedTowers = new List<string>();
    public List<string> selectedEnemies = new List<string>();


    //called after all requirements have been met
    public void Unlock()
    {
        if (icon && locked && defaultSprite)
            icon.sprite = defaultSprite;

        locked = false;
    }


    //called when buying this powerup
    public void Activate()
    {
        if (icon && enabledSprite)
            icon.sprite = enabledSprite;

        if (onEnabledIcon)
            onEnabledIcon.enabled = true;

        ApplyPowerUp();
        enabled = true;
    }


    //activation
    void ApplyPowerUp()
    {
        //get all towers in the game, attached to the TowerManager
        TowerBase[] allBases = TowerManager.instance.gameObject.GetComponentsInChildren<TowerBase>(true);
        //loop through towers and apply powerup
        for (int i = 0; i < allBases.Length; i++)
            ApplyToTower(allBases[i], allBases[i].upgrade);

        //get all spawned enemies in the game, stored properties in the PoolManager
        Properties[] allProps = new Properties[PoolManager.Props.Count];
        PoolManager.Props.Values.CopyTo(allProps, 0);
        //loop through enemies and apply powerup
        for (int i = 0; i < allProps.Length; i++)
            ApplyToEnemy(allProps[i], allProps[i].myMove);

        //apply player-related properties
        ApplyToPlayer();
    }


    public void ApplyToTower(TowerBase tBase, Upgrade upg)
    {
        //only affect selected towers
        if (!selectedTowers.Contains(tBase.transform.parent.name))
            return;

        //TowerBase variables
        //target type
        if(towerProperties.targets.enabled)
            tBase.myTargets = towerProperties.targets.type;
        //shot angle
        if (towerProperties.shotAngle.enabled)
        {
            if (towerProperties.shotAngle.type == TDValue.fix)
                tBase.shotAngle += towerProperties.shotAngle.value;
            else
                tBase.shotAngle = Round(tBase.shotAngle, towerProperties.shotAngle.value);
        }
        //control multiplier
        if (towerProperties.controlMultiplier.enabled)
        {
            if (towerProperties.controlMultiplier.type == TDValue.fix)
                tBase.controlMultiplier += towerProperties.controlMultiplier.value;
            else
                tBase.controlMultiplier = Round(tBase.controlMultiplier, towerProperties.controlMultiplier.value);
        }

        //Upgrade variables, looping through all upgrades
        for (int i = 0; i < upg.options.Count; i++)
        {
            //cache current option
            UpgOptions cur = upg.options[i];

            //damage
            if (towerProperties.damage.enabled)
            {
                if (towerProperties.damage.type == TDValue.fix)
                    cur.damage += towerProperties.damage.value;
                else
                    cur.damage = Round(cur.damage, towerProperties.damage.value);
            }

            //radius
            if (towerProperties.radius.enabled)
            {
                if (towerProperties.radius.type == TDValue.fix)
                    cur.radius += towerProperties.radius.value;
                else
                    cur.radius = Round(cur.radius, towerProperties.radius.value);
            }

            //shot delay
            if (towerProperties.shotDelay.enabled)
            {
                if (towerProperties.shotDelay.type == TDValue.fix)
                    cur.shootDelay += towerProperties.shotDelay.value;
                else
                    cur.shootDelay = Round(cur.shootDelay, towerProperties.shotDelay.value);
            }

            //target count
            if (towerProperties.targetCount.enabled)
                cur.targetCount += towerProperties.targetCount.value;

            //cost
            if (towerProperties.cost.enabled)
            {
                float curValue = towerProperties.cost.initialValue;
                if (i > 0) curValue = towerProperties.cost.value;

                for (int j = 0; j < cur.cost.Length; j++)
                {
                    if (towerProperties.cost.type == TDValue.fix)
                        cur.cost[j] += curValue;
                    else if (towerProperties.cost.costType == CostType.intValue)
                        cur.cost[j] = (int)(cur.cost[j] * curValue);
                    else
                        cur.cost[j] = Round(cur.cost[j], curValue);
                }
            }
        }

        //update tower range collider
        if(towerProperties.radius.enabled)
            upg.RangeChange();
    }


    public void ApplyToEnemy(Properties prop, TweenMove move)
    {
        //only affect selected enemies
        string myName = prop.name;
        if (!selectedEnemies.Contains(myName.Substring(0, myName.Length - 10)))
            return;

        //Properties variables
        //health, substract health and max health
        if (enemyProperties.health.enabled)
        {
            float healthDiff = prop.maxhealth;
            if (prop.maxhealth == 0) prop.maxhealth = prop.health;
            if (enemyProperties.health.type == TDValue.fix)
                prop.maxhealth += enemyProperties.health.value;
            else
                prop.maxhealth = Round(prop.maxhealth, enemyProperties.health.value);
            prop.health -= healthDiff - prop.maxhealth;
            prop.health = Mathf.Clamp(prop.health, 1, prop.maxhealth);
        }

        //shield value, substracts value and max value
        if (enemyProperties.shield.enabled)
        {
            float shieldDiff = prop.shield.maxValue;
            if (prop.shield.maxValue == 0) prop.shield.maxValue = prop.shield.value;
            if (enemyProperties.shield.type == TDValue.fix)
                prop.shield.maxValue += enemyProperties.shield.value;
            else
                prop.shield.maxValue = Round(prop.shield.maxValue, enemyProperties.shield.value);
            prop.shield.value -= shieldDiff - prop.shield.maxValue;
            prop.shield.value = Mathf.Clamp(prop.shield.value, 0, prop.shield.maxValue);
        }

        //refresh hud bars
        if (enemyProperties.health.enabled || enemyProperties.shield.enabled)
            prop.SetUnitFrame();

        //points to earn
        if (enemyProperties.pointsToEarn.enabled)
        {
            for (int j = 0; j < prop.pointsToEarn.Length; j++)
            {
                if (enemyProperties.pointsToEarn.type == TDValue.fix)
                    prop.pointsToEarn[j] += enemyProperties.pointsToEarn.value[j];
                else if (towerProperties.cost.costType == CostType.intValue)
                    prop.pointsToEarn[j] = (int)(prop.pointsToEarn[j] * enemyProperties.pointsToEarn.value[j]);
                else
                    prop.pointsToEarn[j] = Round(prop.pointsToEarn[j], enemyProperties.pointsToEarn.value[j]);
            }
        }

        //damage to deal
        if (enemyProperties.damageToDeal.enabled)
        {
            if (enemyProperties.damageToDeal.type == TDValue.fix)
                prop.damageToDeal += (int)enemyProperties.damageToDeal.value;
            else
                prop.damageToDeal = (int)(prop.damageToDeal * enemyProperties.damageToDeal.value);
        }

        //TweenMove variables
        //speed, permanently changes tween timescale
        if (enemyProperties.speed.enabled)
        {
            float curValue = move.maxSpeed;
            if (enemyProperties.speed.type == TDValue.fix)
                move.maxSpeed += enemyProperties.speed.value;
            else
                move.maxSpeed = Round(move.maxSpeed, enemyProperties.speed.value);
            move.speed = move.maxSpeed;

            if (move.tween != null)
            {
                move.tScale *= move.maxSpeed / curValue;
                if(move.tween.timeScale > move.tScale)
                    move.tween.timeScale = move.tScale;
            }
        }
    }


    public void ApplyToPlayer()
    {
        //add game health
        if(playerProperties.health.enabled)
        {
            //interval is greater than zero, start a coroutine for adding health
            if (playerProperties.health.interval > 0)
            {
                GameHandler.instance.StopCoroutine("AddHealthRoutine");
                GameHandler.instance.StartCoroutine("AddHealthRoutine", playerProperties.health);
            }
            else
            {
                //we haven't defined an interval, one-time add health
                float healthDiff = playerProperties.health.value;
                if (playerProperties.health.type == TDValue.percentual)
                    healthDiff = (int)(GameHandler.maxHealth * playerProperties.health.value);
                GameHandler.AddHealth(healthDiff);
            }
        }

        //add game resources, similar to adding game health
        if(playerProperties.resources.enabled)
        {
            if (playerProperties.resources.interval > 0)
            {
                GameHandler.instance.StopCoroutine("SetResourcesRoutine");
                GameHandler.instance.StartCoroutine("SetResourcesRoutine", playerProperties.resources);
            }
            else
            {
                for (int i = 0; i < playerProperties.resources.value.Length; i++)
                {
                    float resDiff = playerProperties.resources.value[i];
                    if (playerProperties.resources.type == TDValue.percentual)
                        resDiff = (int)(GameHandler.resources[i] * playerProperties.resources.value[i]);
                    GameHandler.SetResources(i, resDiff);
                }
            }
        }
    }


    //helper method to avoid floating point precision issues
    float Round(float value, float multiply)
    {
        return Mathf.Round(value * multiply * 100f) / 100f;
    }


    //tower properties
    [System.Serializable]
    public class PassiveTowerPowerUp
    {
        public Damage damage;
        public Radius radius;
        public ShotDelay shotDelay;
        public TargetCount targetCount;
        public Cost cost;
        public Targets targets;
        public ShotAngle shotAngle;
        public ControlMultiplier controlMultiplier;

        //Upgrade variables
        [System.Serializable]
        public class Damage
        {
            public bool enabled;
            public float value;
            public TDValue type;
        }

        [System.Serializable]
        public class Radius
        {
            public bool enabled;
            public float value;
            public TDValue type;
        }

        [System.Serializable]
        public class ShotDelay
        {
            public bool enabled;
            public float value;
            public TDValue type; 
        }

        [System.Serializable]
        public class TargetCount
        {
            public bool enabled;
            public int value;
        }

        [System.Serializable]
        public class Cost
        {
            public bool enabled;
            public float initialValue;
            public float value;
            public TDValue type;
            public CostType costType;
        }

        //Tower variables
        [System.Serializable]
        public class Targets
        {
            public bool enabled;
            public TowerBase.EnemyType type;
        }

        [System.Serializable]
        public class ShotAngle
        {
            public bool enabled;
            public float value;
            public TDValue type; 
        }

        [System.Serializable]
        public class ControlMultiplier
        {
            public bool enabled;
            public float value;
            public TDValue type;   
        }
    }

    //enemy properties
    [System.Serializable]
    public class PassiveEnemyPowerUp
    {
        public Speed speed;
        public Health health;
        public Shield shield;
        public PointsToEarn pointsToEarn;
        public DamageToDeal damageToDeal;

        //Movement variables
        [System.Serializable]
        public class Speed
        {
            public bool enabled;
            public float value;
            public TDValue type; 
        }

        //Enemy variables
        [System.Serializable]
        public class Health
        {
            public bool enabled;
            public float value;
            public TDValue type;
        }

        [System.Serializable]
        public class Shield
        {
            public bool enabled;
            public float value;
            public TDValue type;
        }

        [System.Serializable]
        public class PointsToEarn
        {
            public bool enabled;
            public float[] value;
            public TDValue type;
            public CostType costType;
        }

        [System.Serializable]
        public class DamageToDeal
        {
            public bool enabled;
            public float value;
            public TDValue type;
        }
    }

    //player properties
    [System.Serializable]
    public class PassivePlayerPowerUp
    {
        public Health health;
        public Resources resources;

        //Game variables
        [System.Serializable]
        public class Health
        {
            public bool enabled;
            public float value;
            public float interval;
            public TDValue type;
        }

        [System.Serializable]
        public class Resources
        {
            public bool enabled;
            public float[] value;
            public float interval;
            public TDValue type;
            public CostType costType;
        }
    }
}