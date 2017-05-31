using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayController : MonoBehaviour {

    public GameObject loginPage;
    public GameObject chatPage;
    public GameObject viewContent;
    public GameObject toggleGroup;
    public RectTransform DialogStartPosition;
    public SimpleObjectPool userButtonPool;
    public SimpleObjectPool timeBoxPool;
    public ChatClient chatClient;
    public Text numUsersText;
    public Text localUserName;
    public GameObject TogglePrefab;
    public GameObject DialogPrefab;


    public toggleControl currentChat;

    private int numU;
    public int numUsers
    {
        get
        {
            return numU;
        }
        set
        {
            numUsersText.text = value.ToString();
            numU = value;
        }
    }

    public void LoadChatRoom()
    {
        loginPage.SetActive(false);
        chatPage.SetActive(true);

        numUsers = chatClient.users.Count;
        localUserName.text = chatClient.userName;

        foreach(var u in chatClient.users.Values)
        {
            GameObject newButton = userButtonPool.GetObject();
            u.userButton = newButton;
            UserButtonControl bControl = newButton.GetComponent<UserButtonControl>();
            bControl.Setup(u);
            newButton.transform.SetParent(viewContent.transform);
        }
    }

    public void DisplayUser(ChatClient.UserItem u)
    {
        GameObject newButton = userButtonPool.GetObject();
        u.userButton = newButton;
        UserButtonControl bControl = newButton.GetComponent<UserButtonControl>();
        bControl.Setup(u);
        newButton.transform.SetParent(viewContent.transform);
        numUsers = numUsers + 1;
    }

    public void UndisplayUser(ChatClient.UserItem u)
    {
        u.convers.dialog.SendButton.interactable = false;
        userButtonPool.ReturnObject(u.userButton);
        u.userButton = null;
        numUsers = numUsers - 1;
    }

    public void createDialog(ChatClient.Conversation conversation)
    {
        //Create toggle for a conversation
        GameObject newtoggle = (GameObject)GameObject.Instantiate(TogglePrefab);
        newtoggle.transform.SetParent(toggleGroup.transform);
        newtoggle.SetActive(true);
        toggleControl newToggleControl = newtoggle.GetComponent<toggleControl>();
        conversation.toggle = newToggleControl;

        //Create Dialog for a conversation
        GameObject newdialog = (GameObject)GameObject.Instantiate(DialogPrefab);
        RectTransform newRect = newdialog.GetComponent<RectTransform>();
        
        newdialog.transform.SetParent(chatPage.transform);
        newRect.localPosition = DialogStartPosition.localPosition;
        conversation.dialog = newdialog.GetComponent<DialogControl>();
        conversation.dialog.SetUp(conversation);

        newToggleControl.SetUp(conversation.remoteName, conversation, this);

        disPlayDialog(conversation);
    }

    public void disPlayDialog(ChatClient.Conversation conversation)
    {
        if(currentChat != null && currentChat != conversation.toggle)
        {
            currentChat.toggle.isOn = false;
        }
        conversation.toggle.toggle.isOn = true;
        currentChat = conversation.toggle;
    }

	// Use this for initialization
	void Start () {
        currentChat = null;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
