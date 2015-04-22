/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;

//emits particles in a circular shape on the x/z axis
public class CircularEmission : MonoBehaviour 
{
    public float radius;        //circle radius
    public int emissionCount;   //amount of particles on the circle
    public float delay;         //delay between emits
    private Vector3 position;   //original position (cached for easier access)
    private ParticleSystem pSystem; //the particle system to use


    //get particle system of this gameobject
    void Start()
    {
        pSystem = GetComponent<ParticleSystem>();
    }


    //method to emit the particles
    //we use OnSpawn() for initialization
	IEnumerator OnSpawn()
    {
        //wait until Start() was executed
        yield return new WaitForEndOfFrame();
        //cache initial particle position
        position = transform.position;

        //emit defined amount of particles
        for (int i = 0; i < emissionCount; i++)
        {
            //calculate current angle of the circle
            float angle = ((float)i / emissionCount) * 360f;
            //get and set corresponding position in a circular shape
            transform.position = GetCirclePos(angle);
            //emit one particle at this position
            pSystem.Emit(1);
            //wait before emitting the next one
            yield return new WaitForSeconds(delay);
        }

        //wait until the last particle died,
        //then clear all particles and despawn this particle effect
        yield return new WaitForSeconds(pSystem.startLifetime);
        pSystem.Clear();
        PoolManager.Pools["Particles"].Despawn(gameObject);
	}


    //returns a position on a circular shape
    //we pass in a angle value, and this method calculates the actual
    //position in world space based on our radius starting from the center
    private Vector3 GetCirclePos(float angle)
    {
        Vector3 pos;
        pos.x = position.x + radius * Mathf.Sin(angle * Mathf.Deg2Rad);
        pos.z = position.z + radius * Mathf.Cos(angle * Mathf.Deg2Rad);
        pos.y = position.y;
        return pos;
    }
}
