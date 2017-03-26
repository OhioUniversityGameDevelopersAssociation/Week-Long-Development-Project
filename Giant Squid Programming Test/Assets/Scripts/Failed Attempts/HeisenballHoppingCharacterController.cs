/* Second try at making a player. Capsule is doesn't have the
 * same flow as rolling, but maybe if we have time, we can make
 * the player hop around to give it a better feel.
 * 
 * The hop just didn't work, I'm going to take the unity animations I build and find a happy
 * medium between sliding and hopping.
 * */
using System;
using System.Collections;
using UnityEngine;

public class HeisenballHoppingCharacterController : MonoBehaviour {

    [Header("Player Movement")]
    [Range(1f, 10f)]
    public float hopForce = 5f;
    public float hopAngle = 10f;
    public float rotationSpeed = 2f;
    [Tooltip("The speed at which we should rotate the ball form back to the capsule rotation when exiting ball form")]
    public float returnToCapsuleSpeed = 2f;

    // reference to the players Rigidbody
    Rigidbody playerRB;
    // Making this variable a part of the controller scope allows us to save from making new Vector3 every frame
    Vector3 movement;
    // Reference to camera so movement is always relative to camera position
    Transform cam;
    Vector3 camForward;
    // Reference to the Animator so we can animate his hops
    Animator anim;
    // Used to know when to give controls back to player
    bool returnedToCapsule = true;
    

    [Header("Jumping")]
    public float jumpForce = 1f;
    public float distToGround = 1f;
    
    // Used to determine when we can are airborne by movement, but not jumping
    bool hopping = false;

    // Input Values. These are updated by unity in Update(), but we want to run physics 
    // in FixedUpdate(), so we'll grab the input we need every frame and pass it to FixedUpdate()
    bool jumpQueued = false;
    bool ballInput = false;
    bool ballInputDown = false;
    bool ballInputUp = false;


    private void Start ()
    {
        // Establish player references
        playerRB = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        cam = Camera.main.transform;
	}

    // Mostly used for input
    private void Update()
    {
        // Get Input from Left stick
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        camForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
        movement = (vertical * camForward + horizontal * cam.right).normalized;
        // Set the animation param to match the movement
        anim.SetBool("Moving", movement != Vector3.zero);

        // I want the player to be able to queue their jump while hopping, so we'll get this
        // input only when the jump isn't queued already

        ballInput = Input.GetButton("Ball Form");
        ballInputDown = Input.GetButtonDown("Ball Form");
        ballInputUp = Input.GetButtonUp("Ball Form");

        if (!jumpQueued || Input.GetButtonUp("Jump"))
            jumpQueued = Input.GetButtonDown("Jump");
        
    }

    private void FixedUpdate()
    {
        // We can do the ball form whenever, so we test that first ..
        if(ballInput)
        {
            anim.SetBool("Ball Form", true);
            // .. and if this is the first frame of being a ball ..
            if (ballInputDown)
            {
                // .. set the appropriate attributes
                anim.SetTrigger("Start Ball");
                playerRB.constraints = RigidbodyConstraints.None;
            }
        }
        else if (ballInputUp)
        {
            StopCoroutine(ReturnToCapsule());
            StartCoroutine(ReturnToCapsule());
        }
        else if (IsGrounded() && returnedToCapsule)
        {
            HandleJump();
            HandleCapsuleMovement();
        }
    }
    

    private void HandleJump()
    {
        if (jumpQueued)
        {
            playerRB.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private bool IsGrounded()
    {
        // Raycast Downward to see if we are on the ground
        bool grounded = Physics.Raycast(transform.position + (Vector3.up * 0.1f), -Vector3.up, distToGround + 0.1f);
        // TODO should this be shorter because of our hops?
        if (hopping)
            hopping = !grounded;
        anim.SetBool("Grounded", grounded);
        return grounded;
    }

    // TODO I want to turn this into a cute little hop instead of a gliding motion
    private void HandleCapsuleMovement()
    {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("TransitionToHang") && !hopping)
        {
            // Move the player to the position
            hopping = true;
            playerRB.AddForce(((Quaternion.Euler(0, hopAngle, 0) * Vector3.forward) + movement) * hopForce);
            //playerRB.MovePosition(playerRB.position + movement * Time.fixedDeltaTime * movementSpeed);
        }

        // Check so that the player doesn't rotate to origin when there is no input
        if (movement != Vector3.zero)
        {
            // Set player direction to movement direction
            playerRB.MoveRotation(Quaternion.LookRotation(movement));
        }
    }

    private IEnumerator ReturnToCapsule()
    {
        if (playerRB.rotation != Quaternion.identity)
        {
            // Flag that we are not finished returning to calsule rotation
            returnedToCapsule = false;

            // Note the rotation we started at when exiting ball form
            Quaternion startingRot = playerRB.rotation;

            // Set up the params measuring return progress
            float startTime = Time.time;
            float journeyLength = Quaternion.Angle(startingRot, Quaternion.identity);

            // until we are are the capsule rotation ..
            while ((Time.time - startTime * returnToCapsuleSpeed) / journeyLength <= 1)
            {
                // Slerp us to it based on the speed
                playerRB.rotation = Quaternion.Slerp(
                    startingRot,
                    Quaternion.identity,
                    Time.time - startTime * returnToCapsuleSpeed / journeyLength);
                // .. and proceed forward a frame
                yield return null;
            }
        }
        // Ensure our player is back at the desired rotation
        playerRB.rotation = Quaternion.identity;

        // Constrain the axes so the player doesn't fall over
        playerRB.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // resume player controls
        returnedToCapsule = true;
        anim.SetBool("Ball Form", false);
    }
}
