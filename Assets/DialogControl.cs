using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class DialogControl : MonoBehaviour {

    public ChatClient.Conversation conversation;
    public SimpleObjectPool messageBoxPool;
    public SimpleObjectPool timeBoxPool;
    public GameObject localContent;
    public GameObject remoteContent;
    public Text inputbox;
    public Button SendButton;

    public void SetUp(ChatClient.Conversation _conversation)
    {
        conversation = _conversation;
        messageBoxPool = GameObject.Find("MessageBoxPool").GetComponent<SimpleObjectPool>();
        timeBoxPool = GameObject.Find("TimeBoxPool").GetComponent<SimpleObjectPool>();
    }

    public void sendMessage()
    {
        conversation.Send(inputbox.text);
        inputbox.text = string.Empty;

        //To be continued... Get a message box from the pool. Display the sent message in a message box
        GameObject localMessage = messageBoxPool.GetObject();
        GameObject remoteEmpty = timeBoxPool.GetObject();

        Text sentMessage = localMessage.GetComponentInChildren<Text>();
        sentMessage.text = inputbox.text;

        Text sentTime = remoteEmpty.GetComponent<Text>();
        sentTime.text = System.DateTime.Now.ToString();

        localMessage.transform.SetParent(localContent.transform);
        localMessage.transform.SetAsLastSibling();
        remoteEmpty.transform.SetParent(remoteContent.transform);
        remoteEmpty.transform.SetAsLastSibling();

        localMessage.SetActive(true);
        remoteEmpty.SetActive(true);

        inputbox.text = string.Empty;
    }

    public void readMessage(string receivedString)
    {
        //To be continued... Get a message box from the poll. Display the received message in a message box
        GameObject remoteMessage = messageBoxPool.GetObject();
        GameObject localEmpty = timeBoxPool.GetObject();

        Text readMessage = remoteMessage.GetComponentInChildren<Text>();
        readMessage.text = receivedString;

        Text readTime = localEmpty.GetComponent<Text>();
        readTime.text = System.DateTime.Now.ToString();

        remoteMessage.transform.SetParent(remoteContent.transform);
        remoteMessage.transform.SetAsLastSibling();
        localEmpty.transform.SetParent(localContent.transform);
        localEmpty.transform.SetAsLastSibling();

        remoteMessage.SetActive(true);
        localEmpty.SetActive(true);

        inputbox.text = string.Empty;
    }

}
