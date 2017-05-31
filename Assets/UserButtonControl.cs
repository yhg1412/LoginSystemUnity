using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserButtonControl : MonoBehaviour {

    public Text username;
    public Text endpoint;
    public ChatClient.UserItem userItem;

    public void Setup(ChatClient.UserItem _userItem)
    {
        userItem = _userItem;
        username.text = userItem.userName;
        endpoint.text = userItem.ip.ToString() + " : " + userItem.port.ToString();
    }

    public void buttonPressed()
    {
        userItem.startChat();
    }
}
