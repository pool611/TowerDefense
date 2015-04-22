/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

//moves the joystick thumb (target) by dragging around the screen, limited to joystick boundaries
//the thumb then transmits its position change to the camera
public class UIJoystick : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{	
	// draggable target object
	public Transform target;

	public float radius = 50f;	// the radius for the joystick to move
	public Vector2 position; 	// [-1, 1] in x,y
    public float speed;			//target drag speed multiplier

    private Vector3 initPos;    //camera start position
    private Vector3 initRot;	//camera start rotation
    private Transform cam;	    //camera transform
    private BoxCollider col;	//collider reference
    private Vector3 initCol;	//stores size of the collider


    //rotation variables
    public float xRotSpeed = 200f;     //rotation speed on x axis
    public float yRotSpeed = 200f;     //rotation speed on y axis
    public float rotDamp = 5f;         //damping factor
    public float yMinRotLimit = -10f;  //y limit upwards
    public float yMaxRotLimit = 60f;   //y limit downwards

    private float xDeg;                     //Mouse X axis input var
    private float yDeg;                     //Mouse Y axis input var
    private Quaternion desiredRotation;     //returned rotation based on xDeg and yDeg
    private Quaternion currentRotation;     //current camera rotation
    private Quaternion rotation;            //rotation with damping ( final rotation )



    void Start()
    {
        cam = Camera.main.transform;	//get camera transform
        initPos = cam.position;         //get starting position
        initRot = cam.eulerAngles;      //get starting rotation
		//get collider and its starting size
        col = gameObject.GetComponent<BoxCollider>();
        initCol = col.size;

        //be sure to grab the current rotations as starting points.
        rotation = currentRotation = desiredRotation = cam.rotation;

        //grab current angle values as starting points, clamp to return positive values
        xDeg = ClampAngle(cam.eulerAngles.y, 0f, 360f);
        yDeg = ClampAngle(cam.eulerAngles.x, 0f, 360f);
    }


    //LateUpdate is called after all Update functions have been called
    void LateUpdate()
    {
        //if we don't control a tower, limit camera height so it stays the same
        if (!SV.control)
        {
			//limit movement to be within the scene bounds
            if (CheckBounds())
                cam.Translate(new Vector3(position.x, 0, position.y) * speed);

            //get current camera position and calculate new height position based on old positions
            Vector3 pos = cam.position;
            pos.y -= (cam.position.y - initPos.y) * 0.1f;
            //assign new position to camera position
            cam.position = pos;

            //keep initial rotation
            xDeg = initRot.y;
            yDeg = initRot.x;
        }
        else
        {
            //check for rotating
            Rotate();
        }
    }


    //whenever we start to drag, fill the screen with the collider
    //to "block" other inputs by that could accidentally happen
    public void OnBeginDrag(PointerEventData data)
    {
        if (!SV.control)
            col.size = new Vector3(Screen.width * 2, Screen.height * 2, col.size.z);
    }


    public void OnDrag(PointerEventData data)
    {
        //get RectTransforms of involved components
        RectTransform draggingPlane = transform as RectTransform;
        RectTransform thumb = target.GetComponent<RectTransform>();
        Vector3 mousePos;

        //check whether the dragged position is inside the dragging rect,
        //then set global mouse position and assign it to the joystick thumb
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(draggingPlane, data.position, data.pressEventCamera, out mousePos))
        {
            thumb.position = mousePos;
        }

        //length of the touch vector (magnitude)
        //calculated from the relative position of the joystick thumb
        float length = target.localPosition.magnitude;

        //if the thumb leaves the joystick's boundaries,
        //clamp it to the max radius
        if (length > radius)
        {
            target.localPosition = Vector3.ClampMagnitude(target.localPosition, radius);
        }

        //set the Vector2 thumb position based on the actual sprite position
        position = target.localPosition;
        //smoothly lerps the Vector2 thumb position based on the old positions
        position = position / radius * Mathf.InverseLerp(radius, 2, 1);
    }


    //we aren't dragging anymore,
    //set the joystick back to the default position
    //and the collider size back to its original size
    public void OnEndDrag(PointerEventData data)
    {
        position = Vector2.zero;
        target.position = transform.position;
        col.size = initCol;
    }


    void Rotate()
    {
        //Set the current joystick input variables multiplied by speed and a slowing factor
        xDeg += position.x * xRotSpeed * 0.02f;
        yDeg -= position.y * yRotSpeed * 0.02f;
		//clamp the angle between min and max rotation
        yDeg = ClampAngle(yDeg, yMinRotLimit, yMaxRotLimit);

        //set camera rotation
        //this returns a rotation that rotates yDeg degrees around x-axis and xDeg degrees around the y-axis
        desiredRotation = Quaternion.Euler(yDeg, xDeg, 0);
        //get current rotation of camera transform
        currentRotation = cam.rotation;

        //Add damping factor, smoothly rotate from currentRotation to desired location in time with that damping
        rotation = Quaternion.Lerp(currentRotation, desiredRotation, Time.deltaTime * rotDamp);
        //assign final rotation to camera
        cam.rotation = rotation;
    }    


    //clamps the angle to always return positive values
    //and make sure it never exceeds given limits
    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360;
        if (angle > 360)
            angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }


    //this method casts a ray against world limit mask with length of 5 units and returns a boolean,
    //which indicates whether movement is possible in that direction
    //on hit: world limit reached - return false, no hit: free space - return true
    private bool CheckBounds()
    {
        Vector3 forw = cam.forward * position.y;
        Vector3 side = cam.right * position.x;

        if (Physics.Raycast(cam.position, forw, 5, SV.worldMask))
        {
            return false;
        }
        else if (Physics.Raycast(cam.position, side, 5, SV.worldMask))
            return false;

        return true;
    }


    //visible gizmo lines in editor for each direction of the camera, with length of 5 units
    //so we see if we touched a movement limit
    void OnDrawGizmos()
    {
		//draw gizmos in blue color
        Gizmos.color = Color.cyan; 

        if (cam)
        {
            Gizmos.DrawRay(cam.position, cam.forward * position.y * 5);
            Gizmos.DrawRay(cam.position, cam.right * position.x * 5);
        }
    }
}

