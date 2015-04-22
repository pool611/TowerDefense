/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

//based on CircularEmission.cs,
//this script orders its children in a circular shape (x-y axis)
public class CircularLayout : MonoBehaviour
{
    public float minOffset;          //circle radius in world units
    private float radius;            //internal position calculation on the circle
    private Vector2 position; //original position (cached for quick access)


    void Awake()
    {
        //init and calculate radius (depending on child count)
        position = GetComponent<RectTransform>().anchoredPosition;
        int childs = transform.childCount;
        radius = childs * minOffset;

        //position defined amount of children
        for (int i = 0; i < childs; i++)
        {
            //calculate current angle of the circle for each child
            float angle = ((float)i / childs) * 360f;
            //get and set corresponding position in a circular shape
            transform.GetChild(i).GetComponent<RectTransform>().anchoredPosition = GetCirclePos(angle);
        }
    }


    //returns a position on a circular shape
    //we pass in a angle value, and this method calculates the actual
    //position in screen space based on our radius starting from the center
    private Vector2 GetCirclePos(float angle)
    {
        Vector2 pos;
        pos.x = position.x + radius * Mathf.Sin(angle * Mathf.Deg2Rad);
        pos.y = position.y + radius * Mathf.Cos(angle * Mathf.Deg2Rad);
        return pos;
    }
}
