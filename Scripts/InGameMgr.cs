using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class InGameMgr : MonoBehaviourPunCallbacks
{
    [Header("--인게임 진입시 변수--")]
    public GameObject MainCamera;

    Dictionary<Vector2, Pos> Dic_pos = new Dictionary<Vector2, Pos>();
    List<Pos> Cur_pos = new List<Pos>();
    public GameObject[] panels;
    public GameObject[] pieces;

    Color32 SelColor = new Color32(86, 255, 249, 255);
    Color32 EnableColor = new Color32(93, 255, 112, 255);
    Color32 AttackColor = new Color32(255, 0, 0, 255);
    Color32 SpecialColor1 = new Color32(255, 117, 236, 255);
    Color32 SpecialColor2 = new Color32(71, 103, 255, 255);
    Color32 PromotionColor = new Color32(255, 0, 255, 255);
    Color32 WhatDidColor = new Color32(255, 255, 122, 255);
    Color32 ReturnColor = new Color32(255, 255, 255, 0);    

    bool Turn = true;
    Vector2 CurVec;
    Vector2 SelectedPos;
    private PhotonView PV;
    Vector2 EnPassantVec;
    bool Chatmode = false;
    int PromoteCursor = 0;
    bool GameOver = false;

    [Header("-- 기본 UI 변수 --")]
    public Text ChatTxt;
    public InputField ChatIFd;
    public Text TurnTxt;
    public Text CheckTxt;
    public Button SurrendarBtn;

    [Header("-- 승급 UI 변수 --")]
    public GameObject SelPanel;
    public Button RookBtn;
    public Button HorseBtn;
    public Button BishopBtn;
    public Button QueenBtn;
    public GameObject[] PromoteObj;

    [Header("-- GameOver UI 변수 --")]
    public GameObject GameOverPanel;
    public Text GameOverTxt;
    public Button GoLobbyBtn;

    // Start is called before the first frame update
    void Start()
    {
        Turn = GlobalValue.MySide;
        PV = GetComponent<PhotonView>();

        if (GlobalValue.MySide == false)
        {
            MainCamera.transform.position = new Vector3(0, 20, 11);
            MainCamera.transform.eulerAngles = new Vector3(65, 180, 0);
            GlobalValue.MySideNum = -1;
        }
        else
        {
            MainCamera.transform.position = new Vector3(0, 20, -11);
            MainCamera.transform.eulerAngles = new Vector3(65, 0, 0);
            GlobalValue.MySideNum = 1;
        }
        
        for(int i=0; i < 64; i++)
        {
            Pos posvalue = new Pos();
            posvalue.tr_x = panels[i].transform.position.x;
            posvalue.tr_z = panels[i].transform.position.z;
            posvalue.Img = panels[i].GetComponent<RawImage>();

            if (i < 16)
            {
                posvalue.piece = pieces[i];
                posvalue.charside = 1;
            }                
            else if (i >= 48)
            {
                posvalue.piece = pieces[i - 32];
                posvalue.charside = -1;
            }
            Cur_pos.Add(posvalue);
            Dic_pos.Add(new Vector2(posvalue.tr_x,posvalue.tr_z), Cur_pos[i]);
        }

        if (Turn)
            TurnTxt.gameObject.SetActive(true);

        RookBtn.onClick.AddListener(() =>
        {
            PromoteCursor = 0;
            PromoteFunc();
        });

        HorseBtn.onClick.AddListener(() =>
        {
            PromoteCursor = 1;
            PromoteFunc();
        });

        BishopBtn.onClick.AddListener(() =>
        {
            PromoteCursor = 2;
            PromoteFunc();
        });

        QueenBtn.onClick.AddListener(() =>
        {
            PromoteCursor = 3;
            PromoteFunc();
        });

        GoLobbyBtn.onClick.AddListener(() =>
        {
            StartCoroutine(NetworkMgr.Inst.RefreshScoreCo());
        });

        SurrendarBtn.onClick.AddListener(() =>
        {
            GameOverFunc(false);
            PV.RPC("GameOverFunc", RpcTarget.Others, true);
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && !GameOver)
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

        if (pieces[4] == null && !GameOver)
        {
            GameOverFunc(!GlobalValue.MySide);
        }
        else if (pieces[28] == null && !GameOver)
        {
            GameOverFunc(GlobalValue.MySide);
        }

        if (Turn != TurnTxt.gameObject.activeSelf)
        {
            TurnTxt.gameObject.SetActive(Turn);
        }

        if (Turn && Input.GetMouseButtonDown(0) && !SelPanel.activeSelf && !GameOverPanel.activeSelf && !GameOver)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.gameObject.CompareTag("Panel"))
                {
                    CurVec = new Vector2(hit.transform.position.x, hit.transform.position.z);
                    if (Dic_pos[CurVec].Img.color == ReturnColor && Dic_pos[CurVec].piece != null)
                    {
                        SelectedPos = CurVec;
                        MoveJudgement(Dic_pos[CurVec]);
                    }
                    else if (Dic_pos[CurVec].Img.color == EnableColor ||
                        Dic_pos[CurVec].Img.color == AttackColor ||
                        Dic_pos[CurVec].Img.color == SpecialColor1 ||
                        Dic_pos[CurVec].Img.color == SpecialColor2)
                    {
                        if (Dic_pos[CurVec].Img.color == SpecialColor1)
                        {
                            PV.RPC("EmPassant", RpcTarget.All,
                                new Vector2(CurVec.x, CurVec.y - 2 * GlobalValue.MySideNum));
                            //TestEmPassant(new Vector2(CurVec.x, CurVec.y - 2 * GlobalValue.MySideNum));//test
                        }
                        else if (Dic_pos[CurVec].Img.color == SpecialColor2)
                        {
                            if (CurVec.x > 0)
                            {
                                PV.RPC("Castling", RpcTarget.All, new Vector2(CurVec.x - 2, CurVec.y));
                                //TestCastling(new Vector2(CurVec.x - 2, CurVec.y));//test
                            }
                            else
                            {
                                PV.RPC("Castling", RpcTarget.All, new Vector2(CurVec.x + 2, CurVec.y));
                                //TestCastling(new Vector2(CurVec.x + 2, CurVec.y));//test
                            }                                
                        }

                        PV.RPC("MovePiece", RpcTarget.All, SelectedPos, CurVec);
                        //TestMovePiece(SelectedPos, CurVec);//test                            
                    }
                    else if (Dic_pos[CurVec].Img.color == PromotionColor)
                    {
                        SelPanel.SetActive(true);
                    }
                }
            }
        }              
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        GameOverFunc(true);
    }

    [PunRPC]
    void GameOverFunc(bool isWin)
    {
        GameOver = true;
        GameOverPanel.SetActive(true);

        if (isWin == false)
            GameOverTxt.text = "You Lose !!";
        else
            StartCoroutine(NetworkMgr.Inst.UpdateScoreCo());
    }

    void MoveJudgement(Pos m_pos)
    {
        float x = m_pos.tr_x;
        float z = m_pos.tr_z;

        ReturnTransparent();
        
        if (m_pos.charside == GlobalValue.MySideNum)
        {
            m_pos.Img.color = SelColor;

            //---------------------------------------------------------------------Pawn
            if (m_pos.piece.name.Contains("Pawn"))
            {
                if (GlobalValue.MySideNum == 1)
                {
                    if (z != 7 && Dic_pos[new Vector2(x, z + 2)].charside == 0)
                    {
                        if (z + 2 == 7)
                            Dic_pos[new Vector2(x, z + 2)].Img.color = PromotionColor;
                        else
                            Dic_pos[new Vector2(x, z + 2)].Img.color = EnableColor;

                        if (Dic_pos[new Vector2(x, z)].ismoved == false)
                        {
                            if (Dic_pos[new Vector2(x, z + 4)].charside == 0)
                                Dic_pos[new Vector2(x, z + 4)].Img.color = EnableColor;
                        }
                    }

                    if (x != -7)
                    {
                        if (Dic_pos[new Vector2(x - 2, z + 2)].charside == -1)
                        {
                            if (z + 2 == 7)
                                Dic_pos[new Vector2(x - 2, z + 2)].Img.color = PromotionColor;
                            else
                                Dic_pos[new Vector2(x - 2, z + 2)].Img.color = AttackColor;
                        }                            
                    }

                    if (x != 7)
                    {
                        if (Dic_pos[new Vector2(x + 2, z + 2)].charside == -1)
                        {
                            if (z + 2 == 7)
                                Dic_pos[new Vector2(x + 2, z + 2)].Img.color = PromotionColor;
                            else
                                Dic_pos[new Vector2(x + 2, z + 2)].Img.color = AttackColor;
                        }                            
                    }

                    if (CheckOut(new Vector2(x + 2, z)))
                    {
                        if (Dic_pos[new Vector2(x + 2, z)].enpassant && Dic_pos[new Vector2(x + 2, z)].charside == -1)
                            Dic_pos[new Vector2(x + 2, z + 2)].Img.color = SpecialColor1;
                    }

                    if (CheckOut(new Vector2(x - 2, z)))
                    {
                        if (Dic_pos[new Vector2(x - 2, z)].enpassant && Dic_pos[new Vector2(x - 2, z)].charside == -1)
                            Dic_pos[new Vector2(x - 2, z + 2)].Img.color = SpecialColor1;
                    }
                }
                else if (GlobalValue.MySideNum == -1)
                {
                    if (z != -7 && Dic_pos[new Vector2(x, z - 2)].charside == 0)
                    {
                        if (z - 2 == -7)
                            Dic_pos[new Vector2(x, z - 2)].Img.color = PromotionColor;
                        else
                            Dic_pos[new Vector2(x, z - 2)].Img.color = EnableColor;                        

                        if (Dic_pos[new Vector2(x, z)].ismoved == false)
                        {
                            if (Dic_pos[new Vector2(x, z - 4)].charside == 0)
                                Dic_pos[new Vector2(x, z - 4)].Img.color = EnableColor;
                        }
                    }

                    if (x != -7)
                    {
                        if (Dic_pos[new Vector2(x - 2, z - 2)].charside == 1)
                        {
                            if (z - 2 == -7)
                                Dic_pos[new Vector2(x - 2, z - 2)].Img.color = PromotionColor;
                            else
                                Dic_pos[new Vector2(x - 2, z - 2)].Img.color = AttackColor;
                        }                            
                    }

                    if (x != 7)
                    {
                        if (Dic_pos[new Vector2(x + 2, z - 2)].charside == 1)
                        {
                            if (z - 2 == -7)
                                Dic_pos[new Vector2(x + 2, z - 2)].Img.color = PromotionColor;
                            else
                                Dic_pos[new Vector2(x + 2, z - 2)].Img.color = AttackColor;
                        }                            
                    }

                    if (CheckOut(new Vector2(x + 2, z)))
                    {
                        if (Dic_pos[new Vector2(x + 2, z)].enpassant && Dic_pos[new Vector2(x + 2, z)].charside == 1)
                            Dic_pos[new Vector2(x + 2, z - 2)].Img.color = SpecialColor1;
                    }
                    if (CheckOut(new Vector2(x - 2, z)))
                    {
                        if (Dic_pos[new Vector2(x - 2, z)].enpassant && Dic_pos[new Vector2(x - 2, z)].charside == 1)
                            Dic_pos[new Vector2(x - 2, z - 2)].Img.color = SpecialColor1;
                    }                    
                }
            }
            //---------------------------------------------------------------------Pawn

            //---------------------------------------------------------------------Rook, Queen
            if (m_pos.piece.name.Contains("Rook") || m_pos.piece.name.Contains("Queen"))
            {
                float cursor = z + 2;
                while (true)
                {
                    if (cursor > 7)
                        break;

                    if (Dic_pos[new Vector2(x, cursor)].charside == 0)
                        Dic_pos[new Vector2(x, cursor)].Img.color = EnableColor;
                    else if (Dic_pos[new Vector2(x, cursor)].charside == GlobalValue.MySideNum)
                        break;
                    else if(Dic_pos[new Vector2(x, cursor)].charside == GlobalValue.MySideNum * -1)
                    {
                        Dic_pos[new Vector2(x, cursor)].Img.color = AttackColor;
                        break;
                    }

                    cursor += 2;
                }

                cursor = z - 2;
                while (true)
                {
                    if (cursor < -7)
                        break;

                    if (Dic_pos[new Vector2(x, cursor)].charside == 0)
                        Dic_pos[new Vector2(x, cursor)].Img.color = EnableColor;
                    else if (Dic_pos[new Vector2(x, cursor)].charside == GlobalValue.MySideNum)
                        break;
                    else if (Dic_pos[new Vector2(x, cursor)].charside == GlobalValue.MySideNum * -1)
                    {
                        Dic_pos[new Vector2(x, cursor)].Img.color = AttackColor;
                        break;
                    }

                    cursor -= 2;
                }

                cursor = x + 2;
                while (true)
                {
                    if (cursor > 7)
                        break;

                    if (Dic_pos[new Vector2(cursor, z)].charside == 0)
                        Dic_pos[new Vector2(cursor, z)].Img.color = EnableColor;
                    else if (Dic_pos[new Vector2(cursor, z)].charside == GlobalValue.MySideNum)
                        break;
                    else if (Dic_pos[new Vector2(cursor, z)].charside == GlobalValue.MySideNum * -1)
                    {
                        Dic_pos[new Vector2(cursor, z)].Img.color = AttackColor;
                        break;
                    }

                    cursor += 2;
                }

                cursor = x - 2;
                while (true)
                {
                    if (cursor < -7)
                        break;

                    if (Dic_pos[new Vector2(cursor, z)].charside == 0)
                        Dic_pos[new Vector2(cursor, z)].Img.color = EnableColor;
                    else if (Dic_pos[new Vector2(cursor, z)].charside == GlobalValue.MySideNum)
                        break;
                    else if (Dic_pos[new Vector2(cursor, z)].charside == GlobalValue.MySideNum * -1)
                    {
                        Dic_pos[new Vector2(cursor, z)].Img.color = AttackColor;
                        break;
                    }

                    cursor -= 2;
                }
            }
            //---------------------------------------------------------------------Rook

            //---------------------------------------------------------------------Horse
            if (m_pos.piece.name.Contains("Horse"))
            {
                if (x <= 5 && z <= 5)
                {
                    if (z <= 3)
                    {
                        if (Dic_pos[new Vector2(x + 2, z + 4)].charside == 0)
                            Dic_pos[new Vector2(x + 2, z + 4)].Img.color = EnableColor;
                        else if (Dic_pos[new Vector2(x + 2, z + 4)].charside == GlobalValue.MySideNum * -1)
                            Dic_pos[new Vector2(x + 2, z + 4)].Img.color = AttackColor;
                    }

                    if (x <= 3)
                    {
                        if (Dic_pos[new Vector2(x + 4, z + 2)].charside == 0)
                            Dic_pos[new Vector2(x + 4, z + 2)].Img.color = EnableColor;
                        else if (Dic_pos[new Vector2(x + 4, z + 2)].charside == GlobalValue.MySideNum * -1)
                            Dic_pos[new Vector2(x + 4, z + 2)].Img.color = AttackColor;
                    }
                }

                if (x <= 5 && z >= -5)
                {
                    if (z >= -3)
                    {
                        if (Dic_pos[new Vector2(x + 2, z - 4)].charside == 0)
                            Dic_pos[new Vector2(x + 2, z - 4)].Img.color = EnableColor;
                        else if (Dic_pos[new Vector2(x + 2, z - 4)].charside == GlobalValue.MySideNum * -1)
                            Dic_pos[new Vector2(x + 2, z - 4)].Img.color = AttackColor;
                    }

                    if (x <= 3)
                    {
                        if (Dic_pos[new Vector2(x + 4, z - 2)].charside == 0)
                            Dic_pos[new Vector2(x + 4, z - 2)].Img.color = EnableColor;
                        else if (Dic_pos[new Vector2(x + 4, z - 2)].charside == GlobalValue.MySideNum * -1)
                            Dic_pos[new Vector2(x + 4, z - 2)].Img.color = AttackColor;
                    }
                }

                if (x >= -5 && z >= -5)
                {
                    if (z >= -3)
                    {
                        if (Dic_pos[new Vector2(x - 2, z - 4)].charside == 0)
                            Dic_pos[new Vector2(x - 2, z - 4)].Img.color = EnableColor;
                        else if (Dic_pos[new Vector2(x - 2, z - 4)].charside == GlobalValue.MySideNum * -1)
                            Dic_pos[new Vector2(x - 2, z - 4)].Img.color = AttackColor;
                    }

                    if (x >= -3)
                    {
                        if (Dic_pos[new Vector2(x - 4, z - 2)].charside == 0)
                            Dic_pos[new Vector2(x - 4, z - 2)].Img.color = EnableColor;
                        else if (Dic_pos[new Vector2(x - 4, z - 2)].charside == GlobalValue.MySideNum * -1)
                            Dic_pos[new Vector2(x - 4, z - 2)].Img.color = AttackColor;
                    }
                }

                if (x >= -5 && z <= 5)
                {
                    if (z <= 3)
                    {
                        if (Dic_pos[new Vector2(x - 2, z + 4)].charside == 0)
                            Dic_pos[new Vector2(x - 2, z + 4)].Img.color = EnableColor;
                        else if (Dic_pos[new Vector2(x - 2, z + 4)].charside == GlobalValue.MySideNum * -1)
                            Dic_pos[new Vector2(x - 2, z + 4)].Img.color = AttackColor;
                    }

                    if (x >= -3)
                    {
                        if (Dic_pos[new Vector2(x - 4, z + 2)].charside == 0)
                            Dic_pos[new Vector2(x - 4, z + 2)].Img.color = EnableColor;
                        else if (Dic_pos[new Vector2(x - 4, z + 2)].charside == GlobalValue.MySideNum * -1)
                            Dic_pos[new Vector2(x - 4, z + 2)].Img.color = AttackColor;
                    }
                }
            }
            //---------------------------------------------------------------------Horse

            //---------------------------------------------------------------------Bishop, Queen
            if (m_pos.piece.name.Contains("Bishop") || m_pos.piece.name.Contains("Queen"))
            {
                float xcursor, zcursor;

                xcursor = x;
                zcursor = z;
                while (true)
                {
                    xcursor += 2;
                    zcursor += 2;

                    if (xcursor > 7 || zcursor > 7)
                        break;

                    if (Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum)
                        break;
                    else if(Dic_pos[new Vector2(xcursor, zcursor)].charside == 0)
                    {
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = EnableColor;
                    }
                    else if(Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum * -1)
                    {
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = AttackColor;
                        break;
                    }
                }

                xcursor = x;
                zcursor = z;
                while (true)
                {
                    xcursor -= 2;
                    zcursor += 2;

                    if (xcursor < -7 || zcursor > 7)
                        break;

                    if (Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum)
                        break;
                    else if (Dic_pos[new Vector2(xcursor, zcursor)].charside == 0)
                    {
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = EnableColor;
                    }
                    else if (Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum * -1)
                    {
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = AttackColor;
                        break;
                    }
                }

                xcursor = x;
                zcursor = z;
                while (true)
                {
                    xcursor += 2;
                    zcursor -= 2;

                    if (xcursor > 7 || zcursor < -7)
                        break;

                    if (Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum)
                        break;
                    else if (Dic_pos[new Vector2(xcursor, zcursor)].charside == 0)
                    {
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = EnableColor;
                    }
                    else if (Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum * -1)
                    {
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = AttackColor;
                        break;
                    }
                }

                xcursor = x;
                zcursor = z;
                while (true)
                {
                    xcursor -= 2;
                    zcursor -= 2;

                    if (xcursor < -7 || zcursor < -7)
                        break;

                    if (Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum)
                        break;
                    else if (Dic_pos[new Vector2(xcursor, zcursor)].charside == 0)
                    {
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = EnableColor;
                    }
                    else if (Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum * -1)
                    {
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = AttackColor;
                        break;
                    }
                }
            }
            //---------------------------------------------------------------------Bishop

            //---------------------------------------------------------------------King
            if (m_pos.piece.name == "King")
            {
                float xcursor = x + 2;
                float zcursor = z;
                if (CheckOut(new Vector2(xcursor, zcursor)))
                {
                    if (Dic_pos[new Vector2(xcursor, zcursor)].charside == 0)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = EnableColor;
                    else if (Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum * -1)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = AttackColor;
                }

                xcursor = x + 2;
                zcursor = z + 2;
                if (CheckOut(new Vector2(xcursor, zcursor)))
                {
                    if (Dic_pos[new Vector2(xcursor, zcursor)].charside == 0)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = EnableColor;
                    else if (Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum * -1)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = AttackColor;
                }

                xcursor = x;
                zcursor = z + 2;
                if (CheckOut(new Vector2(xcursor, zcursor)))
                {
                    if (Dic_pos[new Vector2(xcursor, zcursor)].charside == 0)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = EnableColor;
                    else if (Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum * -1)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = AttackColor;
                }

                xcursor = x - 2;
                zcursor = z + 2;
                if (CheckOut(new Vector2(xcursor, zcursor)))
                {
                    if (Dic_pos[new Vector2(xcursor, zcursor)].charside == 0)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = EnableColor;
                    else if (Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum * -1)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = AttackColor;
                }

                xcursor = x - 2;
                zcursor = z;
                if (CheckOut(new Vector2(xcursor, zcursor)))
                {
                    if (Dic_pos[new Vector2(xcursor, zcursor)].charside == 0)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = EnableColor;
                    else if (Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum * -1)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = AttackColor;
                }

                xcursor = x - 2;
                zcursor = z - 2;
                if (CheckOut(new Vector2(xcursor, zcursor)))
                {
                    if (Dic_pos[new Vector2(xcursor, zcursor)].charside == 0)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = EnableColor;
                    else if (Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum * -1)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = AttackColor;
                }

                xcursor = x;
                zcursor = z - 2;
                if (CheckOut(new Vector2(xcursor, zcursor)))
                {
                    if (Dic_pos[new Vector2(xcursor, zcursor)].charside == 0)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = EnableColor;
                    else if (Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum * -1)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = AttackColor;
                }

                xcursor = x + 2;
                zcursor = z - 2;
                if (CheckOut(new Vector2(xcursor, zcursor)))
                {
                    if (Dic_pos[new Vector2(xcursor, zcursor)].charside == 0)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = EnableColor;
                    else if (Dic_pos[new Vector2(xcursor, zcursor)].charside == GlobalValue.MySideNum * -1)
                        Dic_pos[new Vector2(xcursor, zcursor)].Img.color = AttackColor;
                }

                if (Dic_pos[new Vector2(x, z)].ismoved == false)
                {
                    if (Dic_pos[new Vector2(x + 2, z)].charside == 0 &&
                        Dic_pos[new Vector2(x + 4, z)].charside == 0 &&
                        Dic_pos[new Vector2(x + 6, z)].piece.name == "Rook" &&
                        Dic_pos[new Vector2(x + 6, z)].ismoved == false)
                    {
                        Dic_pos[new Vector2(x + 4, z)].Img.color = SpecialColor2;
                    }

                    if (Dic_pos[new Vector2(x - 2, z)].charside == 0 &&
                        Dic_pos[new Vector2(x - 4, z)].charside == 0 &&
                        Dic_pos[new Vector2(x - 6, z)].charside == 0 &&
                        Dic_pos[new Vector2(x - 8, z)].piece.name == "Rook" &&
                        Dic_pos[new Vector2(x - 8, z)].ismoved == false)
                    {
                        Dic_pos[new Vector2(x - 4, z)].Img.color = SpecialColor2;
                    }
                }
            }
            //---------------------------------------------------------------------King
        }
    }

    bool CheckOut(Vector2 vec)
    {
        if (vec.x > 7 || vec.x < -7 || vec.y > 7 ||vec.y<-7)
            return false;
        else
            return true;
    }

    void ReturnTransparent()
    {
        for(int i = 0; i < 64; i++)
        {
            Cur_pos[i].Img.color = ReturnColor;
        }
    }

    void EnterChat()
    {
        string msg = "\n<color=#ffffff>[" + GlobalValue.Unique_ID + "] : " +
                    ChatIFd.text + "</color>";
        PV.RPC("LogMsg", RpcTarget.All, msg);

        ChatIFd.text = "";
    }

    void PromoteFunc()
    {
        if (GlobalValue.MySideNum == -1)
            PromoteCursor += 4;

        PV.RPC("Promote", RpcTarget.All, SelectedPos, PromoteCursor);
        //TestPromote(SelectedPos, PromoteCursor);//Test

        PV.RPC("MovePiece", RpcTarget.All, SelectedPos, CurVec);
        //TestMovePiece(SelectedPos, CurVec);//test

        SelPanel.SetActive(false);
    }

    void TestPromote(Vector2 SelPos, int Cursor)
    {
        Destroy(Dic_pos[SelPos].piece);
        GameObject newpiece = Instantiate(PromoteObj[Cursor]);
        newpiece.transform.position = new Vector3(SelPos.x, 1.9f, SelPos.y);
        Dic_pos[SelPos].piece = newpiece;

        //PV.RPC("MovePiece", RpcTarget.All, SelectedPos, CurVec);
        TestMovePiece(SelectedPos, CurVec);//test
    }

    void TestMovePiece(Vector2 SelVec, Vector2 TarVec)
    {
        if (Dic_pos[TarVec].piece != null)
            Destroy(Dic_pos[TarVec].piece);

        if (Dic_pos[SelVec].piece.name.Contains("Pawn") && (SelVec - TarVec).magnitude == 4)
        {
            Dic_pos[TarVec].enpassant = true;
            EnPassantVec = TarVec;
        }
        else
        {
            if (EnPassantVec != Vector2.zero)
            {
                Dic_pos[EnPassantVec].enpassant = false;
            }
        }

        Dic_pos[TarVec].piece = Dic_pos[SelVec].piece;
        Dic_pos[TarVec].charside = Dic_pos[SelVec].charside;
        Dic_pos[TarVec].ismoved = true;

        Dic_pos[SelVec].piece.transform.position = new Vector3(TarVec.x, 1.9f, TarVec.y);
        Dic_pos[SelVec].piece = null;
        Dic_pos[SelVec].charside = 0;

        ReturnTransparent();
        Dic_pos[TarVec].Img.color = WhatDidColor;

        GlobalValue.MySideNum *= -1;
    }

    void TestEmPassant(Vector2 TarVec)
    {
        Destroy(Dic_pos[TarVec].piece);
        Dic_pos[TarVec].piece = null;
        Dic_pos[TarVec].charside = 0;
    }

    void TestCastling(Vector2 TarVec)
    {
        if (TarVec.x > 0)
        {
            Dic_pos[TarVec].piece = Dic_pos[new Vector2(7, TarVec.y)].piece;
            Dic_pos[TarVec].charside = Dic_pos[new Vector2(7, TarVec.y)].charside;
            Dic_pos[TarVec].ismoved = true;

            Dic_pos[new Vector2(7, TarVec.y)].piece.transform.position = new Vector3(TarVec.x, 1.9f, TarVec.y);
            Dic_pos[new Vector2(7, TarVec.y)].piece = null;
            Dic_pos[new Vector2(7, TarVec.y)].charside = 0;
        }
        else if (TarVec.x < 0)
        {
            Dic_pos[TarVec].piece = Dic_pos[new Vector2(-7, TarVec.y)].piece;
            Dic_pos[TarVec].charside = Dic_pos[new Vector2(-7, TarVec.y)].charside;
            Dic_pos[TarVec].ismoved = true;

            Dic_pos[new Vector2(-7, TarVec.y)].piece.transform.position = new Vector3(TarVec.x, 1.9f, TarVec.y);
            Dic_pos[new Vector2(-7, TarVec.y)].piece = null;
            Dic_pos[new Vector2(-7, TarVec.y)].charside = 0;
        }
    }

    [PunRPC]
    void MovePiece(Vector2 SelVec, Vector2 TarVec)
    {
        if (Dic_pos[TarVec].piece != null)
            Destroy(Dic_pos[TarVec].piece);

        if (Dic_pos[SelVec].piece.name.Contains("Pawn") && (SelVec - TarVec).magnitude == 4)
        {
            Dic_pos[TarVec].enpassant = true;
            EnPassantVec = TarVec;
        }
        else
        {
            if (EnPassantVec != Vector2.zero)
            {
                Dic_pos[EnPassantVec].enpassant = false;
            }
        }

        Dic_pos[TarVec].piece = Dic_pos[SelVec].piece;
        Dic_pos[TarVec].charside = Dic_pos[SelVec].charside;
        Dic_pos[TarVec].ismoved = true;

        Dic_pos[SelVec].piece.transform.position = new Vector3(TarVec.x, 1.9f, TarVec.y);
        Dic_pos[SelVec].piece = null;
        Dic_pos[SelVec].charside = 0;

        ReturnTransparent();

        Dic_pos[TarVec].Img.color = WhatDidColor;
        Turn = !Turn;
    }

    [PunRPC]
    void EmPassant(Vector2 TarVec)
    {
        Destroy(Dic_pos[TarVec].piece);
        Dic_pos[TarVec].piece = null;
        Dic_pos[TarVec].charside = 0;
    }

    [PunRPC]
    void Castling(Vector2 TarVec)
    {
        if (TarVec.x > 0)
        {
            Dic_pos[TarVec].piece = Dic_pos[new Vector2(7, TarVec.y)].piece;
            Dic_pos[TarVec].charside = Dic_pos[new Vector2(7, TarVec.y)].charside;
            Dic_pos[TarVec].ismoved = true;

            Dic_pos[new Vector2(7, TarVec.y)].piece.transform.position = new Vector3(TarVec.x, 1.9f, TarVec.y);
            Dic_pos[new Vector2(7, TarVec.y)].piece = null;
            Dic_pos[new Vector2(7, TarVec.y)].charside = 0;
        }
        else if (TarVec.x < 0)
        {
            Dic_pos[TarVec].piece = Dic_pos[new Vector2(-7, TarVec.y)].piece;
            Dic_pos[TarVec].charside = Dic_pos[new Vector2(-7, TarVec.y)].charside;
            Dic_pos[TarVec].ismoved = true;

            Dic_pos[new Vector2(-7, TarVec.y)].piece.transform.position = new Vector3(TarVec.x, 1.9f, TarVec.y);
            Dic_pos[new Vector2(-7, TarVec.y)].piece = null;
            Dic_pos[new Vector2(-7, TarVec.y)].charside = 0;
        }
    }

    [PunRPC]
    void Promote(Vector2 SelPos, int Cursor)
    {
        Destroy(Dic_pos[SelPos].piece);
        GameObject newpiece = Instantiate(PromoteObj[Cursor]);
        newpiece.transform.position = new Vector3(SelPos.x, 1.9f, SelPos.y);
        Dic_pos[SelPos].piece = newpiece;
    }    

    [PunRPC]
    void LogMsg(string msg)
    {
        ChatTxt.text = ChatTxt.text + msg;
    }
}
