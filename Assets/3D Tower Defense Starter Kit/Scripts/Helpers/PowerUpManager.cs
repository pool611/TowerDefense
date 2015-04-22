/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


//stores a list of all powerups and triggers their execution
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager instance;
    //list for total battle powerups of each type
    [HideInInspector]
    public List<BattlePowerUp> battlePowerUps = new List<BattlePowerUp>();
    public List<PassivePowerUp> passivePowerUps = new List<PassivePowerUp>();
    //lists for actual classes
    public List<OffensivePowerUp> battleOffensive = new List<OffensivePowerUp>();
    public List<DefensivePowerUp> battleDefensive = new List<DefensivePowerUp>();
    //current active, selected battle/passive powerup
    private BattlePowerUp activePowerUp;
    private PassivePowerUp passivePowerUp;
    //event fired when a battle powerup was executed successfully
    public static event Action<BattlePowerUp> battlePowerUpActivated;
    public static event Action<PassivePowerUp> passivePowerUpActivated;


    void Start()
    {
        instance = this;
    }


    //select powerup based on list indices
    public void SelectPowerUp(int index)
    {
        //new powerup instance for later comparison
        BattlePowerUp powerUp = null;

        //first search the index in the offensive list of powerups,
        //if the index is greater than this list switch to defensive list
        if (index <= battleOffensive.Count - 1)
            powerUp = battleOffensive[index];
        else
            powerUp = battleDefensive[index - battleOffensive.Count];

        //if we selected the same powerup as before,
        //we deselect it again
        if (activePowerUp == powerUp)
            Deselect();
        else
            //else store this powerup as active selection
            activePowerUp = powerUp;
    }


    //select passive powerup based on list indices
    public void SelectPassivePowerUp(int index)
    {
        if (passivePowerUps.Count - 1 < index)
            Debug.LogWarning("Button index " + index + " does not match PowerUps list count.");
        passivePowerUp = passivePowerUps[index];
    }


    //returns whether we have a battle powerup selection
    public bool HasSelection()
    {
        if (activePowerUp != null)
            return true;
        else
            return false;
    }


    //returns whether we have a passive powerup selection
    public bool HasPassiveSelection()
    {
        if (passivePowerUp != null)
            return true;
        else
            return false;
    }


    //return a reference to the active selection
    public BattlePowerUp GetSelection()
    {
        return activePowerUp;
    }


    //return a reference to the passive selection
    public PassivePowerUp GetPassiveSelection()
    {
        return passivePowerUp;
    }


    //unsets the powerup reference
    public void Deselect()
    {
        activePowerUp = null;
    }

    
    //starts a coroutine that executes the selected powerup
    //the corresponding powerup type method is called via overloading
    public void Activate()
    {
        StartCoroutine("ActivatePowerUp", activePowerUp);
    }


    //try to execute offensive powerup
    IEnumerator ActivatePowerUp(OffensivePowerUp powerUp)
    {
        //double check for requirements
        if(!powerUp.CheckRequirements())
            yield break;
        else
            //disable powerup
            powerUp.enabled = false;

        //trigger event notification
        if(battlePowerUpActivated != null)
            battlePowerUpActivated(powerUp);
        //let the powerup handle its own effects
        powerUp.InstantiateFX();
        //delay option execution
        yield return new WaitForSeconds(powerUp.startDelay);

        //handle 'weaken' option
        if (powerUp.weaken.enabled)
            yield return StartCoroutine(powerUp.Weaken());
        //handle 'explosion' option
        if (powerUp.explosion.enabled)
            yield return StartCoroutine(powerUp.Explosion());
        //handle 'burn' option
        if (powerUp.burn.enabled)
            yield return StartCoroutine(powerUp.Burn());
        //handle 'slow' option
        if (powerUp.slow.enabled)
            yield return StartCoroutine(powerUp.Slow());

        //unset target and position for later reuse
        powerUp.target = null;
        powerUp.position = Vector3.zero;

        //wait until the cooldown is over
        //before re-enabling the powerup
        yield return new WaitForSeconds(powerUp.cooldown);
        powerUp.enabled = true;
    }


    //try to execute defensive powerup
    IEnumerator ActivatePowerUp(DefensivePowerUp powerUp)
    {
        //double check for requirements
        if(!powerUp.CheckRequirements())
            yield break;
        else
            //disable powerup
            powerUp.enabled = false;

        //trigger event notification
        if (battlePowerUpActivated != null)
            battlePowerUpActivated(powerUp);
        //let the powerup handle its own effects
        powerUp.InstantiateFX();
        //delay option execution
        yield return new WaitForSeconds(powerUp.startDelay);
        
        //handle boost/buff option
        yield return StartCoroutine(powerUp.BoostTowers());

        //unset target and position for later reuse
        powerUp.target = null;
        powerUp.position = Vector3.zero;

        //wait until the cooldown is over
        //before re-enabling the powerup
        yield return new WaitForSeconds(powerUp.cooldown);
        powerUp.enabled = true;
    }


    //try to execute passive powerup
    public void ActivatePassive()
    {
        //trigger activation
        passivePowerUp.Activate();

        //trigger event notification
        if(passivePowerUpActivated != null)
            passivePowerUpActivated(passivePowerUp);

        //unlock new passive powerups
        UnlockPassive();
    }


    //loops through passive powerups and tries to find new unlocks
    public void UnlockPassive()
    {
        for (int i = 0; i < passivePowerUps.Count; i++)
        {
            PassivePowerUp powerup = passivePowerUps[i];
            bool state = true;
            for (int j = 0; j < powerup.req.Count; j++)
            {
                //found an inactive powerup in the requirements,
                //which means this powerup can't be unlocked yet
                if (!passivePowerUps[powerup.req[j]].enabled)
                {
                    state = false;
                    break;
                }
            }
            //if no inactive powerups were found, unlock
            if(state) powerup.Unlock();
        }
    }


    //static access for applying a powerup to a single tower
    //used when building new towers
    public static void ApplyToSingleTower(TowerBase tBase, Upgrade upg)
    {
        for (int i = 0; i < instance.passivePowerUps.Count; i++)
        {
            if (instance.passivePowerUps[i].enabled)
                instance.passivePowerUps[i].ApplyToTower(tBase, upg);
        }
    }


    //static access for applying a powerup to a single enemy
    //used when spawning new enemies
    public static void ApplyToSingleEnemy(Properties prop, TweenMove move)
    {
        for (int i = 0; i < instance.passivePowerUps.Count; i++)
        {
            if (instance.passivePowerUps[i].enabled)
                instance.passivePowerUps[i].ApplyToEnemy(prop, move);
        }
    }
}