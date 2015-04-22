/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;

//this script handles the timed deactivation of spawned particle prefabs in a pool
public class Pool_Particle : MonoBehaviour
{
    //timer for deactivation
    public float customTimer = 0;
    private ParticleSystem[] pSys;

    void Start()
    {
        //particle system of this gameobject
        //get shuriken particle system once on instantiation (not every time on spawn)
        pSys = GetComponentsInChildren<ParticleSystem>();
        
        //if no custom value is typed in in the inspector - customTimer equals zero,
		//we set this value to the defined duration of this effect
        if (customTimer == 0)
        {
            //this gameobject has shuriken attached
            if (pSys.Length > 0)
            {
                //get the highest duration per particle system
                for (int i = 0; i < pSys.Length; i++)
                {
                    float timeout = pSys[i].duration + pSys[i].startDelay;
                    if (timeout > customTimer)
                        customTimer = timeout;
                }

                
                //and add a half second to ensure it will fully run through
                //otherwise it will abort while running
                customTimer += 0.25f;
            }
            else
            {
                //maybe we decided to use the legacy particle system which uses emitters,
                //so here we try to get this component and reduce one frame we'll wait at OnSpawn()
                //otherwise it will loop again and display artifacts
                ParticleEmitter pEmit = GetComponent<ParticleEmitter>();
                
                if(pEmit)
                    customTimer = GetComponent<ParticleEmitter>().maxEnergy - 0.25f;
                else
                    ////debug warning if shuriken nor an emitter was found
                    Debug.LogWarning("Particle effect: " + gameObject.name + " has no particle system attached!");
            }
        }
    }

	
	//called on every reuse
    IEnumerator OnSpawn()
    {
    	//at instantiation OnStart() and OnSpawn() are called simultaneous,
    	//here we wait one frame so that Start() gets executed before OnSpawn()
        yield return new WaitForEndOfFrame();

        
        for (int i = 0; i < pSys.Length; i++)
        {
            pSys[i].Play();
        }
        

		//wait defined seconds before deactivation
        yield return new WaitForSeconds(customTimer);

        
        for (int i = 0; i < pSys.Length; i++)
        {
            pSys[i].Clear(true);
        }
        
		//despawn this gameobject for later use
        PoolManager.Pools["Particles"].Despawn(gameObject);
    }
}
