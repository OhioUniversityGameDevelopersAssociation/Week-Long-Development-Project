// Camera Controller drives the Camera. This will be a seperate
// rig from the player but will allow us to get around the annoying
// problems involved in parenting the camera

// Choosing to start with the camera controller. Movement will
// Need to change direction based on cameras orientation
// to feel smooth, so better we have this working and work off of that

using System;
using UnityEngine;

public class CameraController : MonoBehaviour {

    [Header("Player Orbiting")]
    // Get Reference to the player
    public Transform player;
    public float sensitivity;
    [Range(-180f, 0f)]
    public float pitchMin = -90f;
    [Range(0f, 180f)]
    public float pitchMax = 90f;
    [Tooltip("The speed at which the camera should move to a position where it is not occluded")]
    public float occludedCameraSpeed = 1f;

    Transform cam;
    // Used to keep the camera rig in correct relation to the player
    Vector3 rigOffset;
    // used in tracking our x rotation and binding it to a certain value
    float PitchRotation = 0f;
    // Used to move the camera back to the correct relative position/rotation to the camera rig
    Vector3 desiredCameraLocalPosition;
    Quaternion desiredCameraLocalRotation;

    // How cool would it be if we kind of flew over the whole of the level, 
    // stopping at important points on our way to the goal
    [Header("Goal Viewing")]
    // Used to store intermediate points we want to see on our way to the goal
    public Transform[] viewPoints;
    // The speed at which we should move to new viewpoints
    public float goalViewSpeed = 1;

    // The current transform on the path we are trying to view
    int currentViewPointIndex = 0;


    private void Start()
    {
        // Get the position of the camera relative to the player
        rigOffset = transform.localPosition - player.localPosition;

        // Get transform of camera in use
        cam = Camera.main.transform;

        // Note the original position/rotation of the camera relative to the parent so we can move back
        // to this placement when we need to
        desiredCameraLocalPosition = cam.localPosition;
        desiredCameraLocalRotation = cam.localRotation;

        if (viewPoints.Length == 0)
            Debug.Log("Don't forget to add goal viewpoints!");
        
    }

    void Update ()
    {
        // TODO Make it so we can break from orbiting the player to view the goal
        if (Input.GetButton("View Goal") && viewPoints.Length > 0)
        {
            ViewGoals();
        }
        else // if we are releasing this, 
        {
            // reset the view point index after viewing goal
            currentViewPointIndex = 0;

            // TODO Should we find a way to make this smoother?
            // Handle Orbiting the Player
            transform.position = player.position + rigOffset;

            RotateCamera();
            FixCameraObstructions();
        }
	}

    private void ViewGoals()
    {
        // An failed version of ViewGoals() is posted at the bottom, it's long and didn't smoothly move into and out of the view
        // points like I wanted it too. I felt like I was on the right track, but I need to budget time for other things.

        // If we're at the goal ..
        if (HasReachedCurrentViewPoint())
        {
            //  .. set a new goal, if there are any
            if (currentViewPointIndex + 1 < viewPoints.Length)
            {
                // Set our sights on a new viewpoint Target
                currentViewPointIndex++;
            }
            else // .. otherwise we just want to hover here
                return;
        }
        else // .. otherwise ..
        {
            // .. move the camera towards the new desired position and angle
            // Lerping in this manner won't actually ever get us to the goal,
            // but it will give us a fairly smoothed move to it and we can measure if we
            // are within a certain distance, as shown in HasReachedCurrentViewPoint()
            cam.position = Vector3.Lerp(
                cam.position, 
                viewPoints[currentViewPointIndex].position, 
                Time.deltaTime * goalViewSpeed);

            cam.rotation = Quaternion.Slerp(
                cam.rotation,
                viewPoints[currentViewPointIndex].rotation,
                Time.deltaTime * goalViewSpeed);

            // Fixing rotation and position of cam after this function
            // stops being called is handled in RotateCamera() 
        }
    }

    // This was an eyesore of an if statement, so this makes it more readable
    private bool HasReachedCurrentViewPoint()
    {
        return Vector3.Distance(cam.position, viewPoints[currentViewPointIndex].position) <= 1f
            && Quaternion.Angle(cam.rotation, viewPoints[currentViewPointIndex].rotation) <= 10f;
    }

    private void RotateCamera()
    {
        // We will be going right into this from 
        cam.localRotation = Quaternion.Slerp(cam.localRotation, desiredCameraLocalRotation, Time.deltaTime * goalViewSpeed);

        // Get input from the right joystick
        float turn = Input.GetAxis("Camera Turn");
        float pitch = Input.GetAxis("Camera Pitch");

        // Apply the joystick's x axis to the object's y rotation in world space
        transform.Rotate(Vector3.up * turn * sensitivity * Time.deltaTime, Space.World);
        

        // We want to bind the cameras pitch angle, so we'll need to keep track of it (since euler angles fail past 360 deg)
        // It isn't perfect but keeping track of this angle ourselves is more reliable than using eulerAngles
        if ((pitch < 0 && PitchRotation > pitchMin) || 
            (pitch > 0 && PitchRotation < pitchMax))
        {
            // Apply the joystick's y axis to the object's x in local space
            transform.Rotate(Vector3.right * pitch * sensitivity * Time.deltaTime);
            PitchRotation += pitch * sensitivity * Time.deltaTime;
        }
        // It's important to keep these coordinate scopes to ensure the camera is always vertical,
        // and turns the appropriate direction
    }

    private void FixCameraObstructions()
    {
        RaycastHit hit;

        // Target that we want our camera to be at. Typically this is the original camera position, so lets set that
        Vector3 targetPosition = desiredCameraLocalPosition;

        // We use a raycast here and ignore the player through the editor. Nothing else requires raycast,
        // so we can just have the player ignore them
        // if something is obstructing the cameras view of the player ..
        Debug.DrawRay(transform.position, -cam.forward * desiredCameraLocalPosition.magnitude, Color.red);
        if(Physics.Raycast(transform.position, -cam.forward, out hit, desiredCameraLocalPosition.magnitude))
        {
            // We want to find a new local position to move the camera to, so we'll take the position
            // at which we hit the other object and find it's position relative to the rig
            targetPosition = transform.InverseTransformPoint(hit.point);

            // If we angle the camera upwards, I want to see the floor, so lets shorten whatever 
            // distance we get by 1/10th of the original distance
            targetPosition *= 0.9f;

            // if for any reason, the camera is going to get too close to the player, don't allow it
            if (targetPosition.magnitude < 1f)
                targetPosition.Normalize();
              
        }

        // Move the camera if it isn't already at the target position
        if (cam.localPosition != targetPosition)
        {
            // Find the normalized direction we need the camera to move
            Vector3 cameraMovement = (targetPosition - cam.localPosition).normalized;

            // Adjust the distance of this movement by how much we want our camera to move per second
            cameraMovement *= Time.deltaTime * occludedCameraSpeed;

            // If this distance would shoot us past the target position ..
            if (cameraMovement.magnitude > Vector3.Distance(cam.localPosition, targetPosition))
            {
                //  .. Just put us at that position
                cam.localPosition = targetPosition;
            }
            else // .. otherwise ..
            {
                // .. add move us by that much towards the target position
                cam.localPosition += cameraMovement;
            }
        }
    }

    /*
    private void ViewGoals()
    {
        // An old version of ViewGoals() is posted at the bottom, it's long and didn't smoothly move into and out of the view
        // points like I wanted it too. I felt like I was on the right track, but I need to budget time for other things.
        // When dealing with the first goal as the edge case, we'll need to make sure to calculate the journeyMidPoint
        // from the camera's original position
        if(Input.GetButtonDown("View Goal"))
        {
            prevViewpointPosition = cam.position;
            prevViewpointRotation = cam.rotation;
        }

        // If we're at the goal ..
        if (HasReachedCurrentViewPoint())
        {
            //  .. set a new goal, if there are any
            if (currentViewPointIndex + 1 < viewPoints.Length)
            {
                // Set our sights on a new viewpoint Target
                currentViewPointIndex++;

                prevViewpointPosition = viewPoints[currentViewPointIndex - 1].position;
                prevViewpointRotation = viewPoints[currentViewPointIndex - 1].rotation;

                journeyProgress = 0f;

                // Take note of where the midpoint is, as we want to move fastest there,
                // and slowest when the viewpoint is in frame
                    journeymidPoint = Vector3.Distance(
                        viewPoints[currentViewPointIndex - 1].position,
                        viewPoints[currentViewPointIndex].position) / 2;
            }
            else // .. otherwise we just want to hover here
                return;
        }
        else // .. otherwise ..
        {
            // Calculate the our current progress along that path, and scale it based on whether we are close to the end or not

            // Determine the speed at which we should move the camera to slow in and out of view points
            // This is based off of how close to the center of our journey we are
            float goalViewSpeed = Mathf.Lerp(
                goalViewMaxSpeed,
                goalViewLowSpeed,
                (Mathf.Abs(Vector3.Distance(cam.position, viewPoints[currentViewPointIndex].position) - journeymidPoint) / journeymidPoint));
            // .. move the camera towards the new desired position and angle

            journeyProgress += Time.deltaTime * goalViewSpeed;



            cam.position = Vector3.Lerp(
                prevViewpointPosition,
                viewPoints[currentViewPointIndex].position,
                journeyProgress);

            cam.rotation = Quaternion.Slerp(
                prevViewpointRotation,
                viewPoints[currentViewPointIndex].rotation,
                journeyProgress);
        }
    }
    */
}
