/*  This file is part of the "3D Tower Defense Starter Kit" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using System.Collections;


public class CameraControl : MonoBehaviour
{
    //scroll variables
    public float scrollSpeed = 15f; //scroll speed factor
    public bool edgeScroll;     //should camera scroll near edges
    public float scrollEdge = 0.01f;    //mouse position distance to edge
    //pan variable
    public float panSpeed = 10f;    //pan speed factor

    //zoom variables
    public Vector2 zoomRange = new Vector2(-5,5);   //possible range to zoom up and down
    public float zoomSpeed = 1f;    //zoom speed factor
    private float currentZoom = 0;  //old zoom factor

    private Vector3 initPos;    //camera start position


    //rotation variables
    public float xRotSpeed = 200f;     //rotation speed on x axis
    public float yRotSpeed = 200f;     //rotation speed on y axis
    public float rotDamp = 5f;   //damping factor
    public float yMinRotLimit = -10f;  //y limit upwards
    public float yMaxRotLimit = 60f;  //y limit downwards

    private float xDeg;     //Mouse X axis input var
    private float yDeg;     //Mouse Y axis input var
    private Quaternion desiredRotation;     //returned rotation based on xDeg and yDeg
    private Quaternion currentRotation;     //current camera rotation
    private Quaternion rotation;    //rotation with damping ( final rotation )


	//initialize start position and rotation angles
    public void Start()
    {
        initPos = transform.position;   //get starting position

        //be sure to grab the current rotations as starting points.
        rotation = transform.rotation;
        currentRotation = transform.rotation;
        desiredRotation = transform.rotation;

        //grab current angle values as starting points, clamp to return positive values
        xDeg = ClampAngle(transform.eulerAngles.y, 0f, 360f);
        yDeg = ClampAngle(transform.eulerAngles.x, 0f, 360f);
    }


    //LateUpdate is called after all Update functions have been called
    void LateUpdate()
    {
        //PAN
        //only middle mouse button is pressed ( exclude left and right mouse button )
        if (Input.GetKey("mouse 2") && !Input.GetMouseButton(0) && !Input.GetMouseButton(1))
        {
            //left and right pan movement
            //mouse position is smaller or bigger than screen width middle and camera is within bounds
            //(there is space to move left in this direction)
            //then translate camera position in this direction, in time, with given speed, in world space.
            if ( Input.mousePosition.x <= Screen.width * 0.5f && CheckBounds(-transform.right)
                 || Input.mousePosition.x >= Screen.width * 0.5f && CheckBounds(transform.right))
            {
                transform.Translate(transform.right * Time.deltaTime * panSpeed * 
                (Input.mousePosition.x - Screen.width * 0.5f) / (Screen.width * 0.5f), Space.World);
            }

            //forward and back pan movement
            //mouse position is smaller or bigger than screen height middle and camera pos is within bounds
            //similiar to above
            if ( Input.mousePosition.y <= Screen.height * 0.5f && CheckBounds(-transform.forward)
                 || Input.mousePosition.y >= Screen.height * 0.5f && CheckBounds(transform.forward))
            {
                transform.Translate(transform.forward * Time.deltaTime * panSpeed *
                (Input.mousePosition.y - Screen.height * 0.5f) / (Screen.height * 0.5f), Space.World);
            }
        }
        else
        {
            //middle mouse button is not pressed, look up if edgeScroll is enabled:
            if (edgeScroll)
            {
                //right edge scroll, mouse position is bigger than invisible edge, and camera pos is within bounds
                if (Input.mousePosition.x >= Screen.width * (1 - scrollEdge) && CheckBounds(transform.right))
                {
                    //translate camera position in this direction, in time, with given speed, in world space.
                    transform.Translate(transform.right * Time.deltaTime * scrollSpeed, Space.World);
                }
                //left edge scroll
                else if (Input.mousePosition.x <= Screen.width * scrollEdge && CheckBounds(-transform.right))
                {
                    transform.Translate(transform.right * Time.deltaTime * -scrollSpeed, Space.World);
                }

                //forward edge scroll
                if (Input.mousePosition.y >= Screen.height * (1 - scrollEdge) && CheckBounds(transform.forward))
                {
                    transform.Translate(transform.forward * Time.deltaTime * scrollSpeed, Space.World);
                }
                //backward edge scroll
                else if (Input.mousePosition.y <= Screen.height * scrollEdge && CheckBounds(-transform.forward))
                {
                    transform.Translate(transform.forward * Time.deltaTime * -scrollSpeed, Space.World);
                }
            }
            else    //edgeScroll is disabled, so scrolling is possible with arrow keys:
            {
                //if desired direction key is pressed and camera is still within world limit bounds,
                //translate camera position in this direction, in time, with given speed, in world space.
                if (Input.GetKey("right") && CheckBounds(transform.right))
                {
                    transform.Translate(transform.right * Time.deltaTime * scrollSpeed, Space.World);
                }
                else if (Input.GetKey("left") && CheckBounds(-transform.right))
                {
                    transform.Translate(transform.right * Time.deltaTime * -scrollSpeed, Space.World);
                }

                if (Input.GetKey("up") && CheckBounds(transform.forward))
                {
                    transform.Translate(transform.forward * Time.deltaTime * scrollSpeed, Space.World);
                }
                else if (Input.GetKey("down") && CheckBounds(-transform.forward))
                {
                    transform.Translate(transform.forward * Time.deltaTime * -scrollSpeed, Space.World);
                }
            }
        }

        //ZOOM IN/OUT
        //while left mouse button is not pressed, get current zoom position
        if (!Input.GetMouseButton(0))
        {
            //scroll wheel input with given speed determines desired zoom position
            currentZoom -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * 1000 * zoomSpeed;
            //make sure that zoom position is still in zoomRange limit
            currentZoom = Mathf.Clamp(currentZoom, zoomRange.x, zoomRange.y);
            //get current camera position and calculate new zoomed in/out position based on old positions
            Vector3 pos = transform.position;
            pos.y -= (transform.position.y - (initPos.y + currentZoom)) * 0.1f;
            //assign new position to camera position
            transform.position = pos;
        }

        //check for rotating
        Rotate();
    }


    public void Rotate()
    {
        //ROTATION
        //check if right mouse button is pressed 
        if (Input.GetMouseButton(1))
        {            
            //Set the current axis input variables multiplied by speed and a slowing factor
            xDeg += Input.GetAxis("Mouse X") * xRotSpeed * 0.02f;
            yDeg -= Input.GetAxis("Mouse Y") * yRotSpeed * 0.02f;
        }

        //enable arrow key rotation in self control mode
        if (SV.control)
        {
            //right arrow key pressed
            if (Input.GetKey("right"))
            {
                //set x axis input multiplied by scrolling and rotation speed + slowing factor
                xDeg += Time.deltaTime * scrollSpeed * xRotSpeed * 0.02f;
            }
            else if (Input.GetKey("left"))  //left arrow key
            {
                //set x axis input in other direction, negative scrollSpeed
                xDeg += Time.deltaTime * -scrollSpeed * xRotSpeed * 0.02f;
            }

            //down arrow key pressed
            if (Input.GetKey("down"))
            {
                //set y axis input
                yDeg += Time.deltaTime * scrollSpeed * yRotSpeed * 0.02f;
            }
            else if (Input.GetKey("up"))    //up arrow key
            {
                //set y axis input in other direction, negative scrollSpeed
                yDeg += Time.deltaTime * -scrollSpeed * yRotSpeed * 0.02f;
            }
        }

        //for each possible input: right mouse button / self control mode, adjust rotation
        if (Input.GetMouseButton(1) || SV.control)
        {
            //Make sure that yDeg is still in the y limits
            //Clamp the vertical axis for the orbit
            yDeg = ClampAngle(yDeg, yMinRotLimit, yMaxRotLimit);

            //set camera rotation
            //this returns a rotation that rotates yDeg degrees around x-axis and xDeg degrees around the y-axis
            desiredRotation = Quaternion.Euler(yDeg, xDeg, 0);
            //get current rotation of camera transform
            currentRotation = transform.rotation;

            //Add damping factor, smoothly rotate from currentRotation to desired location in time with that damping
            rotation = Quaternion.Lerp(currentRotation, desiredRotation, Time.deltaTime * rotDamp);
            //assign final rotation to camera
            transform.rotation = rotation;
        }
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


    //this method casts a ray against the world limit mask with length of 5 units and returns a boolean,
    //which indicates whether movement is possible in that direction
    //on hit: world limit reached - return false, no hit: free space - return true
    private bool CheckBounds(Vector3 direction)
    {
        if (Physics.Raycast(transform.position, direction, 5, SV.worldMask))
        {
            return false;
        }
        else
            return true;
    }


    //visible gizmo lines in editor for each direction of the camera, with length of 5 units
    //so we see if we touched a movement limit
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan; //draw gizmos in blue color
        Gizmos.DrawRay(transform.position, transform.right * 5);
        Gizmos.DrawRay(transform.position, -transform.right * 5);
        Gizmos.DrawRay(transform.position, transform.forward * 5);
        Gizmos.DrawRay(transform.position, -transform.forward * 5);
    }
}
