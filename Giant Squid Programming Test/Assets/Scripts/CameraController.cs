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
    // Used to make track how far the camera is from the player based on obstruction
    float desiredCameraDistance;

    [Header("Goal Viewing")]
    // Used to store intermediate points we want to see on our way to the goal
    public Transform[] viewPoints;

    private void Start()
    {
        // Get the position of the camera relative to the player
        rigOffset = transform.localPosition - player.localPosition;

        // Get transform of camera in use
        cam = Camera.main.transform;
        // Find the distance from the rig that we want the camera to sit
        desiredCameraDistance = Vector3.Distance(transform.position, cam.position);
    }

    void Update ()
    {
        // TODO Make it so we can break from orbiting the player to view the goal

        // Handle Orbiting the Player
        transform.position = player.position + rigOffset;
        RotateCamera();

        FixCameraObstructions();
	}

    private void RotateCamera()
    {
        
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
        Vector3 targetPosition;

        // if something is obstructing the cameras view of the player
        Debug.DrawRay(transform.position, -cam.forward, Color.red, desiredCameraDistance);
        if(Physics.Raycast(transform.position, -cam.forward, out hit, desiredCameraDistance))
        {
            // Set our target potision
            float newDistance = Vector3.Distance(transform.position, hit.transform.position);

            // Since the camera is parented to the camera rig, we can make a vector that just moves the
            // local z position forward and backwards based on the distance we need.
            targetPosition = new Vector3(
                cam.localPosition.x,
                cam.localPosition.y,
                newDistance);   
        }
        else
        {
            // if we are not obstructed, move back to the desired distance
            targetPosition = new Vector3(
                cam.localPosition.x,
                cam.localPosition.y,
                desiredCameraDistance);
        }

        // Move the camera if it isn't already at the target position
        if (Vector3.Distance(cam.position, transform.position) != desiredCameraDistance)
        {
            // Find the normalized direction we need the camera to move
            Vector3 cameraMovement = (targetPosition - cam.localPosition).normalized;

            // Adjust the distance of this movement by how much we want our camera to move per second
            cameraMovement *= Time.deltaTime * occludedCameraSpeed;

            // If this distance would shoot us past the target position ..
            if (cameraMovement.magnitude > Vector3.Distance(transform.position, targetPosition))
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
}
