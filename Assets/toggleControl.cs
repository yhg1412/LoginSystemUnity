using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class toggleControl : MonoBehaviour {

    public Text userName;
    public ChatClient.Conversation conversation;
    private DisplayController display;
    public Toggle toggle;

    public void SetUp(string _userName, ChatClient.Conversation _conversation, DisplayController _display)
    {
        userName.text = _userName;
        conversation = _conversation;
        display = _display;
        toggle.isOn = true;
    }

    public void onPressed(bool state)
    {
        if (state)
        {
            conversation.dialog.gameObject.SetActive(true);
            display.disPlayDialog(conversation);
        }
        else
        {
            conversation.dialog.gameObject.SetActive(false);
        }
    }
}
