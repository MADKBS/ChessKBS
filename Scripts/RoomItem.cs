using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomItem : MonoBehaviour
{
    public Text RoomName;
    [HideInInspector] public int playerCount;

    // Start is called before the first frame update
    void Start()
    {
        this.GetComponent<Button>().onClick.AddListener(() =>
        {
            FindObjectOfType<LobbyMgr>().OnClickedRoomItem(RoomName.text, playerCount);
            FindObjectOfType<LobbyMgr>().MakeRoomPanel.SetActive(false);
        });
    }

    public void DispRoomData(string RN)
    {
        RoomName.text = RN;
    }
}
