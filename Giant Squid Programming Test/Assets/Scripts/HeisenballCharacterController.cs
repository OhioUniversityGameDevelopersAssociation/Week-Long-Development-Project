/* Second try at making a player. Capsule is doesn't have the
 * same flow as rolling, but maybe if we have time, we can make
 * the player hop around to give it a better feel
 * */
using System;
using System.Collections;
using UnityEngine;

public class HeisenballCharacterController : MonoBehaviour {

    [Header("Player Movement")]
    /*[Range(1f, 100f)]*/
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
    public bool jumpInProgress = true;
    public float distToGround = 0.1f;       // The distance from the our ground-checking raycast will travel to look for ground

    // Input Values. These are updated by unity in Update(), but we want to run physics 
    // in FixedUpdate(), so we'll grab the input we need every frame and pass it to FixedUpdate()
    bool jumpQueued = false;

    public bool tempBool;


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
        // Unfortunately, input events update in Update() in Unity. So if we want to get GetButtonDown() or
        // GetButtonDown(). We are only tracking the jumpin this manner because if we used GetButton, we would
        // simply miss jump inputs, if we simply used GetButtonDown(), we . We would be doing everything in Update, 
        // but the physics system operates in FixedUpdate(). We phrase it this way because Update typically runs
        // faster than 
        if (!jumpQueued && !jumpInProgress)
        {
            jumpQueued = Input.GetButtonDown("Jump");
        }
    }

    private void FixedUpdate()
    {

        // If we are entering or exiting the ball form ..
        if (Input.GetButton("Ball Form") && !isBall)
        {
            BecomeBall();
        }
        // if we have met all the criteria for turning into a capsule
        else if (!Input.GetButton("Ball Form") && isBall)
        {
            BecomeCapsule();
        }

        // Get Input from Left stick
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        camForward = Vector3.Scale(cam.forward, new Vector3(1, 0, 1)).normalized;
        movement = (vertical * camForward + horizontal * cam.right);

        // Set the animation param to match the movement (we do this in FixedUpdate() instead of Update() because the animator is set to match physics
        anim.SetBool("Moving", movement != Vector3.zero);
        anim.SetFloat("MovementSpeed", movement.magnitude);

        

        // If we are in Capsule Form ..
        if (!isBall && returnedToCapsule)
        {
            HandleCapsuleMovement();
            HandleJumping();
        }
        else // if we are currently in ball form
        {
            playerRB.AddForce(movement * ballMovementSpeed);
        }

        // We want to reset jump input at the end of each fixed update frame, to make sure we only jump when we need to
        jumpQueued = false;

        
    }

    private void HandleJumping()
    {
        // If we are on the ground, and are attempting to jump ..
        if (IsGrounded() && jumpQueued && !jumpInProgress)
        {
            /* .. Jump!
            jumpInProgress = true;
            StartCoroutine(Jump());*/
            anim.SetTrigger("Jump");
            jumpInProgress = true;
        }
        else if (jumpInProgress && anim.GetCurrentAnimatorStateInfo(0).IsName("TransitionToHang"))
        {
            // Apply upwards force to the player when it appears we should in the animation
            playerRB.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            jumpInProgress = false;
        }
    }

    private bool IsGrounded()
    {
        // First, Raycast Downward to see if we are on the ground
        bool grounded = Physics.Raycast(transform.position, -Vector3.up, distToGround);
        anim.SetBool("Grounded", grounded);

        // When mashing the A button, we noticed we could get a higher jump because 
        // multiple jump coroutines would get started at the same time. To solve this,
        // we are going to make sure that our 
        //if(anim.GetCurrentAnimatorStateInfo(0).IsName("TransitionToHang"))

        return grounded;
    }

    // TODO I want to turn this into a cute little hop instead of a gliding motion
    private void HandleCapsuleMovement()
    {

        playerRB.MovePosition(playerRB.position + movement * Time.fixedDeltaTime * movementSpeed);
        /*
        movement *= Time.fixedDeltaTime * movementSpeed;
        playerRB.velocity = new Vector3(movement.x, playerRB.velocity.y, movement.z);
        Debug.Log(playerRB.velocity);
        */
        // Check so that the player doesn't rotate to origin when there is no input
        if (movement != Vector3.zero)
        {
            // Set player direction to movement direction
            playerRB.MoveRotation(Quaternion.LookRotation(movement));
        }
    }

    private void BecomeCapsule()
    {
        // Set our flag so we know we are no longer in ball form
        isBall = false;

        // Constrain the axes so the player doesn't fall over
        playerRB.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        StartCoroutine(ReturnToCapsule());
    }

    private void BecomeBall()
    {
        // .. Start the animation and free our rotation axes
        if(!anim.GetCurrentAnimatorStateInfo(0).IsName("EnterBallForm") &&
            !anim.GetCurrentAnimatorStateInfo(0).IsName("BallForm") &&
            !anim.GetCurrentAnimatorStateInfo(0).IsName("ExitBallForm"))
        anim.SetTrigger("Start Ball");
        playerRB.constraints = RigidbodyConstraints.None;
        isBall = true;
    }

    private IEnumerator ReturnToCapsule()
    {
        // Flag that we are not yet in capsule form
        returnedToCapsule = false;

        // If we are not back to an upright rotation ..
        if (playerRB.rotation != Quaternion.identity)
        {

            // Note the rotation we started at when exiting ball form
            Quaternion startingRot = playerRB.rotation;

            // Set up the params measuring return progress
            float startTime = Time.time;
            float journeyLength = Quaternion.Angle(startingRot, Quaternion.identity);

            // Until we are at the proper capsule rotation ..
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

            // When finished, Ensure our player is back at the desired rotation
            playerRB.rotation = Quaternion.identity;
        }
        
        

        // resume player controls
        returnedToCapsule = true;

        // Set the animator to return to the capsule
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("EnterBall") ||
            anim.GetCurrentAnimatorStateInfo(0).IsName("BallForm") || (anim.IsInTransition(0) && anim.GetAnimatorTransitionInfo(0).anyState))
        {
            anim.SetTrigger("Exit Ball");
        }
    }

    /*private void RotateBackToCapsule()
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
    }*/

    // An attempt to keep player from mashing A to get higher jumps. Honestly an utter failure, and was 
    // the longest thing to make a creative fix for. I tried using a coroutine to wait to
    // apply the jump force until the end of an animation. Finally realized, I can queue it without using
    // Coroutines.
    /*private IEnumerator Jump()
    {
        // Set us to play our jump animation in the editor
        anim.SetTrigger("Jump");
        
        // While we are waiting for the actuall lift of the ground part of the animation ..
        while (!anim.GetCurrentAnimatorStateInfo(0).IsName("TransitionToHang"))
        {
            // .. do nothing
            yield return null;
        }

        
    }*/
}
