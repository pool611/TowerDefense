/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using Holoville.HOTween;
using Holoville.HOTween.Plugins;


//moves a few particles along a path,
//so that the path gets partially visible for the player
public class PathIndicator : MonoBehaviour
{
    //particle system reference
    private ParticleSystem pSys;
    //movement script reference
    private TweenMove myMove;
    //path to follow
    public PathManager pathToFollow;


    void Start()
    {
        //store shuriken particle system and movement script
        pSys = GetComponentInChildren<ParticleSystem>();
        myMove = GetComponent<TweenMove>();
        //set path of movement script
        myMove.pathContainer = pathToFollow;
        myMove.maxSpeed = myMove.speed;
        //let this object follow the path
        myMove.StartCoroutine("OnSpawn");
        //start emitting particles while moving
        StartCoroutine("EmitParticles");
    }


    //endless loop for spawning particles in short delays
    IEnumerator EmitParticles()
    {
        //wait movement script to be ready
        yield return new WaitForEndOfFrame();
        //start loop
        while (true)
        {
            //emit one particle
            pSys.Emit(1);
            //wait before emitting another one
            yield return new WaitForSeconds(0.2f);
        }
    }


    //make use of the message send by TweenMove
    //when reaching the end of the path, start all over again
    void PathEnd()
    {
        //start movement from the beginning
        myMove.StartMove();
    }
}
