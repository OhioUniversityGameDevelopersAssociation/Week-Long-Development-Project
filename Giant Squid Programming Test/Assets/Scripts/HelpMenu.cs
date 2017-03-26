/*
 * Simple script just meant to turn the help menu on and off
 * */
using UnityEngine;

public class HelpMenu : MonoBehaviour {

    public GameObject startingMessage;

    public GameObject helpMenu;
    

    void Update ()
    {
		if(Input.GetButtonDown("Help"))
        {
            // turn off the starting message
            startingMessage.SetActive(false);
            // if the help menu is off, turn it on, if it's on, turn it off
            helpMenu.SetActive(!helpMenu.activeSelf);
        }
	}
}
