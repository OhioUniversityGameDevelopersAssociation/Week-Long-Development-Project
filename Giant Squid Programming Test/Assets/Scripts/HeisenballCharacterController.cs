/* Second try at making a player. Capsule is doesn't have the
 * same flow as rolling, but maybe if we have time, we can make
 * the player hop around to give it a better feel
 * */
using System;
using System.Collections;
using UnityEngine;

public class HeisenballCharacterController : MonoBehaviour {

    [Header("Player Movement")]
    [Range(1f, 10f)]
    public float movementSpeed = 5f;        // The speed at which our character should move in capsule form
    // TODO Fix this
    public float rotationSpeed = 2f;        // The speed at which our character should move to face the movement direction in capsule form
    [Tooltip("The speed at which we should rotate the ball form back to the capsule rotation when exiting ball form")]
    public float ballMovementSpeed = 1f;
    public float returnToCapsuleSpeed = 2f; // How long it should take us to return to the upright Rotation
    
    Rigidbody playerRB;                     // reference to the players Rigidbody
    Vector3 movement;                       // Making this variable a part of the controller scope allows us to save from making new Vector3 every frame
    Transform cam;                          // Reference to camera so movement is always relative to camera position
    Vector3 camForward;                     // Used in scaling the movement vector to allow our player to go in the proper direction
    Animator anim;                          // Reference to the Animator so we can animate his hops
    bool isBall = false;                            // Flag if we are currently in ball form
    bool returnedToCapsule = true;          // Used to know when to give controls back to player

    [Header("Jumping")]
    public float jumpForce = 1f;            // The force in meters per second we apply to our character when jumping
    public bool jumpInProgress = false;
    public float distToGround = 0.1f;       // The distance from the our ground-checking raycast will travel to look for ground

    // Input Values. These are updated by unity in Update(), but we want to run physics 
    // in FixedUpdate(), so we'll grab the input we need every frame and pass it to FixedUpdate()
    bool jumpQueued = false;
    bool ballInput = false;


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


        ballInput = Input.GetButton("Ball Form");

        // I want the player to be able to queue their jump while hopping, so we'll get this
        // input only when the jump isn't queued already
        if (!jumpQueued)
        {
            jumpQueued = Input.GetButtonDown("Jump");
        }
    }

    private void FixedUpdate()
    {
        // Set the animation param to match the movement (we do this in FixedUpdate() instead of Update() because the animator is set to match physics
        anim.SetBool("Moving", movement != Vector3.zero);

        // If we are entering or exiting the ball form ..
        if (ballInput && !isBall)
        {
            BecomeBall();
        }
        else if(!ballInput && isBall)
        {
            BecomeCapsule();
        }

        // If we are in Capsule Form ..
        if (!isBall && returnedToCapsule)
        {
            HandleCapsuleMovement();

            // If we are on the ground, not already starting a jump, and are attempting to jump ..
            if (IsGrounded() && jumpQueued && !jumpInProgress)
            {
                // .. Jump!
                StartCoroutine(Jump());
            }
        }
        else // if we are currently in ball form
        {
            playerRB.AddForce(movement * ballMovementSpeed);
        }

        // We want to reset jump input at the end of each fixed update frame, to make sure we only jump when we need to
        jumpQueued = false;
    }

    private void RotateBackToCapsule()
    {
        // Rotate towards the identity rotation
        playerRB.rotation = Quaternion.Slerp(playerRB.rotation, Quaternion.identity, Time.fixedDeltaTime * returnToCapsuleSpeed);
        // When we are fairly close to being upright ..
        if (Quaternion.Angle(playerRB.rotation, Quaternion.identity) < 10f)
        {
            // .. Set the player fully upright
            playerRB.rotation = Quaternion.identity;
            // Start the animation back to capsule
            anim.SetTrigger("Exit Ball");
            // Constrain the axes so the player doesn't fall over

            // Set flag as being finished with returning upright
            returnedToCapsule = true;
        }
    }

    private IEnumerator Jump()
    {
        // We need to make sure this Coroutine doesn't get started again next physics tick, so we'll set a flag to make sure we don't 
        jumpInProgress = true;
        // Reset our jump flag
        jumpQueued = false;

        // Set us to play our jump animation in the editor
        anim.SetTrigger("Jump");

        

        // While we are waiting for the actuall lift of the ground part of the animation ..
        while (!anim.GetCurrentAnimatorStateInfo(0).IsName("TransitionToHang"))
        {
            // .. do nothing
            yield return null;
        }

        // Reset our flag to let us know we can start a new jump next time we are grounded
        jumpInProgress = false;

        // Apply upwards force to the player when it appears we should in the animation
        playerRB.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private bool IsGrounded()
    {
        // Raycast Downward to see if we are on the ground
        bool grounded = Physics.Raycast(transform.position + (Vector3.up * 0.1f), -Vector3.up, distToGround + 0.1f);
        anim.SetBool("Grounded", grounded);
        return grounded;
    }

    // TODO I want to turn this into a cute little hop instead of a gliding motion
    private void HandleCapsuleMovement()
    {
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("TransitionToHang"))
        {
            // Move the player to the position
            //hopping = true;
            //playerRB.AddForce(((Quaternion.Euler(0, hopAngle, 0) * Vector3.forward) + movement) * hopForce);
            
        }

        //movement *= Time.fixedDeltaTime * movementSpeed;

        playerRB.MovePosition(playerRB.position + movement * Time.fixedDeltaTime * movementSpeed);

        // Check so that the player doesn't rotate to origin when there is no input
        if (movement != Vector3.zero)
        {
            // Set player direction to movement direction
            playerRB.MoveRotation(Quaternion.LookRotation(movement));
        }
    }

    private void BecomeCapsule()
    {
        StartCoroutine(ReturnToCapsule());
        isBall = false;
    }

    private void BecomeBall()
    {
        // .. Start the animation and free our rotation axes
        anim.SetTrigger("Start Ball");
        playerRB.constraints = RigidbodyConstraints.None;
        isBall = true;
    }

    private IEnumerator ReturnToCapsule()
    {
        returnedToCapsule = false;

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
            while (((Time.time - startTime) * returnToCapsuleSpeed) / journeyLength <= 1)
            {
                // Slerp us to it based on the speed
                playerRB.rotation = Quaternion.Slerp(
                    startingRot,
                    Quaternion.identity,
                    ((Time.time - startTime) * returnToCapsuleSpeed) / journeyLength);
                // .. and proceed forward a frame
                yield return null;
            }

            // Ensure our player is back at the desired rotation
            playerRB.rotation = Quaternion.identity;
        }
        
        // Constrain the axes so the player doesn't fall over
        playerRB.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // resume player controls
        returnedToCapsule = true;

        // Set the animator to return to the capsule
        anim.SetTrigger("Exit Ball");
    }
}
