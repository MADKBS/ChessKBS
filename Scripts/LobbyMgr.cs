using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class LobbyMgr : MonoBehaviourPunCallbacks
{
    [Header("--기본 UI 관련 변수--")]
    public Button LogOutBtn;
    public Button MakeRoomBtn;
    public Text ChatTxt;
    public InputField ChatIFd;
    public GameObject LoadingPanel;

    [Header("--상태 메시지 관련 변수--")]
    public Text Info_Txt;

    [Header("--방목록 관련 변수--")]
    public GameObject JoinRoomPanel;
    public GameObject scrollContents;
    public GameObject roomItem;
    public List<RoomInfo> myList = new List<RoomInfo>();

    [Header("--방 정보 관련 변수--")]
    public GameObject InfoRoomPanel;
    public Text IRP_RoomNameTxt;
    public Text IRP_IsFullTxt;
    public Button Join_Btn;

    [Header("--방 생성 관련 변수--")]
    public GameObject MakeRoomPanel;
    public InputField MRP_RoomNameIFd;
    public Button Build_Btn;

    [Header("--방 상태 관련 변수--")]
    public GameObject CurrentRoomPanel;
    public Text CRP_RoomNameTxt;
    public Text WhiteSideTxt;
    public Text BlackSideTxt;
    public Button ChangeSideBtn;
    public Button OutBtn;
    public Button PlayBtn;

    string JoinRoomName;
    private PhotonView PV;
    bool Chatmode = false;

    public Button TestBtn;

    // Start is called before the first frame update
    void Start()
    {
        if (GlobalValue.isLogined)
            LoadingPanel.gameObject.SetActive(false);

        PV = GetComponent<PhotonView>();
        GlobalValue.MySide = true;

        RefreshMyInfo();

        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();

        MRP_RoomNameIFd.text = GlobalValue.Unique_ID + "'s Room";

        MakeRoomBtn.onClick.AddListener(() =>
        {
            MakeRoomPanel.SetActive(true);
        });

        Build_Btn.onClick.AddListener(CreateRoom);

        OutBtn.onClick.AddListener(() =>
        {
            PhotonNetwork.LeaveRoom();
            JoinRoomPanel.SetActive(true);
            CurrentRoomPanel.SetActive(false);
            GlobalValue.Host = false;
        });

        Join_Btn.onClick.AddListener(() =>
        {
            PhotonNetwork.JoinRoom(JoinRoomName);
            Debug.Log(JoinRoomName + "에 접속했습니다");
        });

        ChangeSideBtn.onClick.AddListener(() =>
        {
            PV.RPC("ChangeSide", RpcTarget.All);
        });

        LogOutBtn.onClick.AddListener(() =>
        {
            PhotonNetwork.Disconnect();
            SceneManager.LoadScene("Title");
        });

        PlayBtn.onClick.AddListener(() =>
        {
            PV.RPC("LoadGameScene", RpcTarget.All);
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Chatmode = !Chatmode;

            if (Chatmode)
            {
                ChatIFd.gameObject.SetActive(true);
                ChatIFd.ActivateInputField();
            }
            else
            {
                ChatIFd.gameObject.SetActive(false);

                if (ChatIFd.text != "")
                    EnterChat();
            }
        }
    }

    void EnterChat()
    {
        string msg = "\n<color=#ffffff>[" + GlobalValue.Unique_ID + "] : " +
                    ChatIFd.text + "</color>";
        PV.RPC("LogMsg", RpcTarget.All, msg);

        ChatIFd.text = "";
    }

    void RefreshMyInfo()
    {
        Info_Txt.text = "환영합니다. " + GlobalValue.Unique_ID +
                   " 님..   승수 : " + GlobalValue.WinCount + "  패배수 : " +
                   GlobalValue.LoseCount;
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("서버 접속 완료");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("로비 접속 완료");
        PhotonNetwork.LocalPlayer.NickName = GlobalValue.Unique_ID;
        LoadingPanel.gameObject.SetActive(false);
        GlobalValue.isLogined = true;
        myList.Clear();
    }

    public void CreateRoom()
    {
        string roomName = MRP_RoomNameIFd.text;
        if (string.IsNullOrEmpty(MRP_RoomNameIFd.text))
            roomName = GlobalValue.Unique_ID + "'s Room";

        PhotonNetwork.LocalPlayer.NickName = GlobalValue.Unique_ID;

        RoomOptions rmOptions = new RoomOptions();
        rmOptions.IsOpen = true;
        rmOptions.IsVisible = true;
        rmOptions.MaxPlayers = 2;

        PhotonNetwork.CreateRoom(roomName, rmOptions, TypedLobby.Default);
        MakeRoomPanel.SetActive(false);
        GlobalValue.Host = true;
        GlobalValue.MySide = true;
    }

    public override void OnJoinedRoom()
    {
        CurrRoomSet();
        JoinRoomPanel.SetActive(false);
        InfoRoomPanel.SetActive(false);
    }

    void CurrRoomSet()
    {
        if (CurrentRoomPanel.activeSelf == false)
            CurrentRoomPanel.SetActive(true);

        Room currRoom = PhotonNetwork.CurrentRoom;
        CRP_RoomNameTxt.text = currRoom.Name;

        if (currRoom.PlayerCount == 2)
        {
            for(int ii = 0; ii < 2; ii++)
            {
                if (PhotonNetwork.PlayerList[ii].NickName == GlobalValue.Unique_ID)
                    continue;

                GlobalValue.Opponent_ID = PhotonNetwork.PlayerList[ii].NickName;
            }

            if (GlobalValue.Host)
            {
                ChangeSideBtn.gameObject.SetActive(true);
                PlayBtn.gameObject.SetActive(true);
            }
            else
            {
                ChangeSideBtn.gameObject.SetActive(false);
                PlayBtn.gameObject.SetActive(false);
            }
        }
        else
        {
            GlobalValue.Opponent_ID = "";

            if (GlobalValue.Host)
            {
                ChangeSideBtn.gameObject.SetActive(false);
                PlayBtn.gameObject.SetActive(false);
            }
        }

        if (GlobalValue.MySide)
        {
            WhiteSideTxt.text = GlobalValue.Unique_ID;
            BlackSideTxt.text = GlobalValue.Opponent_ID;
        }
        else
        {
            WhiteSideTxt.text = GlobalValue.Opponent_ID;
            BlackSideTxt.text = GlobalValue.Unique_ID;
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {    
        for (int ii = 0; ii < roomList.Count; ii++)
        {
            if (!roomList[ii].RemovedFromList)
            {
                if (!myList.Contains(roomList[ii])) myList.Add(roomList[ii]);
                else myList[myList.IndexOf(roomList[ii])] = roomList[ii];
            }
            else if (myList.IndexOf(roomList[ii]) != -1)
                myList.RemoveAt(myList.IndexOf(roomList[ii]));
        }

        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("ROOM_ITEM"))
        {
            Destroy(obj);
        }

        for (int ii = 0; ii < myList.Count; ii++)
        {
            if (myList[ii].RemovedFromList)
                continue;

            GameObject room = (GameObject)Instantiate(roomItem);
            room.transform.SetParent(scrollContents.transform, false);

            RoomItem rmItem = room.GetComponent<RoomItem>();
            rmItem.DispRoomData(myList[ii].Name);
            rmItem.playerCount = myList[ii].PlayerCount;
        }
    }

    public void OnClickedRoomItem(string roomName, int playerCount)
    {
        if (InfoRoomPanel.activeSelf == false)
            InfoRoomPanel.SetActive(true);

        IRP_RoomNameTxt.text = roomName;
        
        if (playerCount == 1)
        {
            Join_Btn.gameObject.SetActive(true);
            IRP_IsFullTxt.gameObject.SetActive(false);
        }
        else
        {
            Join_Btn.gameObject.SetActive(false);
            IRP_IsFullTxt.gameObject.SetActive(true);
        }

        JoinRoomName = roomName;
        GlobalValue.MySide = false;
        GlobalValue.Host = false;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        CurrRoomSet();
        LogMsg("\n<color=#ff789d>[시스템 메시지] : " + newPlayer.NickName +
            " 님이 방에 입장했습니다.</color>");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        CurrRoomSet();
        LogMsg("\n<color=#ff789d>[시스템 메시지] : " + otherPlayer.NickName +
            " 님이 방에서 퇴장했습니다.</color>");

        if (GlobalValue.Host == false)
        {
            PhotonNetwork.LeaveRoom();
            JoinRoomPanel.SetActive(true);
            CurrentRoomPanel.SetActive(false);
        }
    }

    [PunRPC]
    void ChangeSide()
    {
        GlobalValue.MySide = !GlobalValue.MySide;
        CurrRoomSet();
        PV.RPC("LogMsg", RpcTarget.Others, "\n<color=#84FF7F>[시스템 메시지] : " +
            "말의 색이 변경되었습니다.</color>");
    }

    [PunRPC]
    void LogMsg(string msg)
    {
        ChatTxt.text = ChatTxt.text + msg;
    }

    [PunRPC]
    void LoadGameScene()
    {
        SceneManager.LoadScene("InGame");
    }
}
