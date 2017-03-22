/* First Try at a player controller, I can't say I've tried this before,
*  But I love the ideal of a cube rolling around and climbing on things.
*  Going to first try just making a cube roll around.
*  
*  Stopped after trying to move it with different movement speeds applied,
*  Didn't feel good, needed a lot of force to get going, careens away when
*  we finally get enough force. Just doesn't feel worth it, trying with
*  another character
*  */
using UnityEngine;

public class RollingCubePlayerController : MonoBehaviour {

    public float movementSpeed;

    // Reference to the player Rigidbody
    Rigidbody playerRB;
    
	void Start ()
    {
        playerRB = GetComponent<Rigidbody>(); 
	}
	
	void FixedUpdate ()
    {
        // Get Input values
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Push the cube using these forces to make it roll over its edges smoothly
        playerRB.AddForce((transform.forward * vertical) + (transform.right * horizontal) * movementSpeed);
    }
}
