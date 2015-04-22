/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


//this script is responsible for all playback of audio files
public class AudioManager : MonoBehaviour
{
    //audio source to play sound on
    private static AudioSource audioSource;
    //reference to this (own) script for oneshot sounds, they need a base script to run on
    private static AudioManager reference;

    public GameObject oneShotPrefab;


    void Awake()
    {
		//set reference to this script
        reference = this;
        //get audio source component attached to this gameobject
        audioSource = gameObject.GetComponent<AudioSource>();

		//debug a warning if no audio source was attached and it's null
        if (!audioSource)
            Debug.LogWarning("Audio Manager has no Audio Source component attached!");
    }

	
	//play sound clip passed in on attached audio source
	//when the audio source is set to loop within the inspector,
	//this method can be used for looping background music
    public static void Play(AudioClip clip)
    {
    	//abort execution clip wasn't set or music already playing
        if (clip == null || audioSource.clip == clip) return;

		//assign audio clip to audio source
        audioSource.clip = clip;
        //play assigned audio clip
        audioSource.Play();
    }


    //play sound clip passed in in 3D space
    //we automatically create an audio source to play it - this also uses our PoolManager
    public static void Play(AudioClip clip, Vector3 position)
    {
        //abort execution if clip wasn't set
        if (clip == null) return;

        //spawn/activate new audio gameobject from pool
        GameObject audioObj = PoolManager.Pools["Audio"].Spawn(reference.oneShotPrefab, position, Quaternion.identity);
        //get and store audio source for later use
        AudioSource source = audioObj.GetComponent<AudioSource>();
        //assign passed in clip
        source.clip = clip;
        //play clip
        source.Play();
        //despawn audio gameobject when the clip stops playing
        PoolManager.Pools["Audio"].Despawn(audioObj, clip.length);
    }


    //play sound clip passed in in 3D space with some pitch
    //we automatically create an audio source for being able to access its
    //pitch component and adjust that - this also uses our PoolManager
    public static void Play(AudioClip clip, Vector3 position, float pitch)
    {
        //abort execution if clip wasn't set
        if (clip == null) return;

        //spawn/activate new audio gameobject from pool
        GameObject audioObj = PoolManager.Pools["Audio"].Spawn(reference.oneShotPrefab, position, Quaternion.identity);
        //get and store audio source for later use
        AudioSource source = audioObj.GetComponent<AudioSource>();
        //assign passed in clip
        source.clip = clip;
        //set pitch value
        source.pitch = pitch;
        //play clip with pitch
        source.Play();
        //despawn audio gameobject when the clip stops playing
        PoolManager.Pools["Audio"].Despawn(audioObj, clip.length);
    }
    

    //play sound clip passed in only one time
    //for 2D sounds! to play 3D sounds use Play(clip, position)
    //PlayOneShot() requires a reference to run on 
    public static void Play2D(AudioClip clip)
    {
        //abort execution if clip wasn't set
        if (clip == null) return;

        //play oneshot clip on this reference
        reference.audio.PlayOneShot(clip);
    }


	//pause audio clip currently assigned to our audio source
	//resume playback by calling 'Continue()'
    public static void Pause()
    {
        audioSource.Pause();
    }
    
    
    //resume paused audio clip assigned to this audio source
    public static void Continue()
    {
    	audioSource.Play();
    }


	//pause audio clip currently assigned to our audio source
	//and automatically resume it after 'seconds' seconds
    public static IEnumerator PauseForSeconds(float seconds)
    {
    	//pause audio clip currently running
        audioSource.Pause();
		
		//wait passed in seconds
        yield return new WaitForSeconds(seconds);

		//resume audio clip playback
        audioSource.Play();
    }

	
	//stop all audio clips currently running on this source
    public static void Stop()
    {
    	//abort all looping methods
        reference.StopAllCoroutines();
        //stop audio source clip
        audioSource.Stop();
    }
}


//class for pool name and contained audio clips
//'_AudioClips' is a list of this class which gets
//display via the inspector for easy access
[System.Serializable]
public class AudioClips
{
	//pool name used as access structure
    public string poolName = "";
    //array of audio clips in this pool
    public AudioClip[] clips;
}
