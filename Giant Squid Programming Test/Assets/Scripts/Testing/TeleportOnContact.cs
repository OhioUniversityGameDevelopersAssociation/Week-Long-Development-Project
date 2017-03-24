using UnityEngine;

// Just used to reset the player when we step on a debug room goal

public class TeleportOnContact : MonoBehaviour {
    public Transform destination;

    Transform controller, cameraRig;
    private void Start()
    {
        controller = GameObject.FindObjectOfType<HeisenballCharacterController>().transform;
        cameraRig = Camera.main.transform.parent;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player" && destination)
        {
            // Reset Player Position
            controller.position = destination.position;
            controller.rotation = destination.rotation;

            // Don't bother moving the camera if the player wants to view the goal
            if (!Input.GetButton("View Goal"))
            {
                // Reset Camera Position
                cameraRig.position = destination.position;
                cameraRig.rotation = destination.rotation;
            }
        }
    }
}
