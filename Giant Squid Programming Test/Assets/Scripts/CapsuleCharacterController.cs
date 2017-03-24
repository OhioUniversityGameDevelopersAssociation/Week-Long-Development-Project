/* Second try at making a player. Capsule is doesn't have the
 * same flow as rolling, but maybe if we have time, we can make
 * the player hop around to give it a better feel
 * 
 * This isn't broken in any way, it's just not interesting, going
 * to add super duper simple animations to make him look more alive
 * and implement the rolling
 * */
using System;
using UnityEngine;

public class CapsuleCharacterController : MonoBehaviour {

    [Header("Player Movement")]
    [Range(1f, 10f)]
    public float movementSpeed = 5;
    public float rotationSpeed = 2;

    // reference to the players Rigidbody
    Rigidbody playerRB;
    // Making this variable a part of the controller scope allows us to save from making new Vector3 every frame
    Vector3 movement;
    // Reference to camera so movement is always relative to camera position
    Transform cam;
    Vector3 camForward;

    [Header("Jumping")]
    public float jumpForce = 1f;
    public float distToGround = 1f;

	void Start ()
    {
        // Establish player references
        playerRB = GetComponent<Rigidbody>();

        cam = Camera.main.transform;
	}
	
	
	void FixedUpdate ()
    {
        HandleCapsuleMovement();
        HandleJump();
    }

    private void HandleJump()
    {
        if (IsGrounded())
        {
            if (Input.GetButtonDown("Jump"))
            {
                Jump();
            }
        }
    }

    private void Jump()
    {
        // TODO Implement Jump
        playerRB.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position + (Vector3.up * 0.1f), -Vector3.up, distToGround + 0.1f);
    }

    // TODO I want to turn this into a cute little hop instead of a gliding motion
    private void HandleCapsuleMovement()
    {
        // Previous rotation fix that attempted to smoothly change directions is at bottom. Simply hopped between different
        // rotations very fast, removed it and copied this function with that implementation as the bottom.

        // Get Input from Left stick
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // TODO Make movement relative to camera position
        camForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
        movement = (vertical * camForward + horizontal * cam.right).normalized;

        // Move the player to the position
        playerRB.MovePosition(playerRB.position + movement * Time.fixedDeltaTime * movementSpeed);
        
        // Check so that the player doesn't rotate to origin when there is no input
        if (movement != Vector3.zero)
        {
            // I tried this for rotation and it didn't really work, might come back to it, in the meantime, I'll
            // Just have the player face the movement direction immediately
            /*float y = Quaternion.Angle(playerRB.rotation, Quaternion.LookRotation(movement));

            // if the angle is greater than going half way around ..
            if (y > 180f)
            {
                // .. it would be shorter to go the opposite direction, to we reverse the value to get the same rotation in the shortest path
                y -= 360f;
            }

            Debug.Log(y);
            // Smooth the rotation time
            y *= Time.fixedDeltaTime * rotationSpeed;

            // Rotate the player to the new position
            playerRB.MoveRotation(playerRB.rotation * Quaternion.Euler(0f, y, 0f));
            */

            // Set player direction to movement direction
            playerRB.MoveRotation(Quaternion.LookRotation(movement));
        }
        
    }

    /* Failed Capsule movement first attempt. Rotation was terrible
    private void HandleCapsuleMovement()
    {
        // Previous rotation fix that attempted to smoothly change directions is at bottom. Simply hopped between different
        // rotations very fast, removed it and copied this function with that implementation as the bottom.

        // Get Input from Left stick
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // TODO Make movement relative to camera position
        camForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
        movement = (vertical * camForward + horizontal * cam.right).normalized;

        // Move the player to the position
        playerRB.MovePosition(playerRB.position + movement * Time.fixedDeltaTime * movementSpeed);
        
        // Check so that the player doesn't rotate to origin when there is no input
        if (movement != Vector3.zero)
        {
            // I tried this for rotation and it didn't really work, might come back to it, in the meantime, I'll
            // Just have the player face the movement direction immediately
            /*float y = Quaternion.Angle(playerRB.rotation, Quaternion.LookRotation(movement));

            // if the angle is greater than going half way around ..
            if (y > 180f)
            {
                // .. it would be shorter to go the opposite direction, to we reverse the value to get the same rotation in the shortest path
                y -= 360f;
            }

            Debug.Log(y);
            // Smooth the rotation time
            y *= Time.fixedDeltaTime * rotationSpeed;

            // Rotate the player to the new position
            playerRB.MoveRotation(playerRB.rotation * Quaternion.Euler(0f, y, 0f));
        }
    }
    */
}
