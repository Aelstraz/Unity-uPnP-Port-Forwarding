using UnityEngine;
using UnityEngine.UI;

public class ExampleScript : MonoBehaviour {
    public Text natStatusText;
    public Text errorLogText;
    public Text debugLogText;
    public InputField portNumberInputField;
    public Button startPortForwardButton;
    public Button removePortForwardButton;

    public void Start()
    {
        //Turn on optional debug mode and error logging mode for more detailed information while testing. If used, it must be enabled before starting port forwarding in order to function.
        uPnPHelper.DebugMode = true;
        uPnPHelper.LogErrors = true;
    }

    private void OnGUI()
    {
        SetNATStatus();
        //Returns all debug/error messages generated in session as a single formatted string.
        debugLogText.text = uPnPHelper.GetDebugMessages();
        errorLogText.text = uPnPHelper.GetErrorMessages();
    }

    public void StartButton_Click()
    {
        int portNumber = int.Parse(portNumberInputField.text);

        //Starts automatically forwarding the requested port asynchronously.
        //Takes a string as the protocol which is either "TCP" or "UDP".
        //Takes an integer as the desired port number to forward.
        //Takes an integer as the desired lifetime of the port (0 = infinite).
        //Takes a string as the description. This is shown by some routers so you know what port forward is for what. (e.g. Team Fortress Port Forward.)        
        uPnPHelper.Start(uPnPHelper.Protocol.UDP, portNumber, 0, "Unity uPnP Port Forward Test.");
    }

    public void RemoveButton_Click()
    {
        //Removes any previously forwarded ports this session and closes connections.
        uPnPHelper.CloseAll();
    }

    //Sets the text and color of the NAT status text depending on its state. Also enables/disables buttons dependent on NAT status.
    private void SetNATStatus()
    {
        uPnPHelper.NatType status = uPnPHelper.GetNATType();
        natStatusText.text = "NAT Status: " + status;
        if (status == uPnPHelper.NatType.Checking)
        {
            natStatusText.color = Color.yellow;
            //Disables buttons if the nat status is 'Checking' so that they can't be spammed and break stuff.
            startPortForwardButton.interactable = false;
            removePortForwardButton.interactable = false;
        }
        else if (status == uPnPHelper.NatType.Closed || status == uPnPHelper.NatType.Failed)
        {
            natStatusText.color = Color.red;
            //Re-enables buttons in case they were disabled from the checking status.
            startPortForwardButton.interactable = true;
            removePortForwardButton.interactable = true;
        }
        else if (status == uPnPHelper.NatType.Open)
        {
            natStatusText.color = Color.green;
            //Re-enables buttons in case they were disabled from the checking status.
            startPortForwardButton.interactable = true;
            removePortForwardButton.interactable = true;
        }
    }
}