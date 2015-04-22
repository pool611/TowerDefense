/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

//progress map object element property class
public class ProgressMapObject : MonoBehaviour
{  
    public Slider slider; //bar for manipulating the progress value
    public UnityEngine.UI.Image image; //sprite component that shows the texture icon
    //object alive sprite name - moving icon on the progress bar
    public Sprite objAliveSprite;
    //object killed sprite name - we display this texture on enemy death
    public Sprite objDeadSprite;


    //when spawned, change the current sprite to the 'alive' one
    void OnSpawn()
    {
        //workaround for Unity bug setting the rect position to something else
        RectTransform rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        slider.value = 0f;
        image.sprite = objAliveSprite;
    }


    //executed by ProgressMap.cs
    public void CalculateProgress(float currentProgress)
    {
        //set object's progress
        slider.value = currentProgress;
    }
}