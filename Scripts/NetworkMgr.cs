using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class NetworkMgr : MonoBehaviour
{
    string UpdateScoreUrl;
    string RefreshScoreUrl;
    public static NetworkMgr Inst;

    // Start is called before the first frame update
    void Awake()
    {
        UpdateScoreUrl = "http://cholong1993.dothome.co.kr/Chess/UpdateScore.php";
        RefreshScoreUrl = "http://cholong1993.dothome.co.kr/Chess/RefreshScore.php";
        Inst = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator UpdateScoreCo()
    {
        WWWForm form = new WWWForm();
        form.AddField("My_ID", GlobalValue.Unique_ID,
                                        System.Text.Encoding.UTF8);
        form.AddField("OP_ID", GlobalValue.Opponent_ID,
                                        System.Text.Encoding.UTF8);

        UnityWebRequest a_www = UnityWebRequest.Post(UpdateScoreUrl, form);
        yield return a_www.SendWebRequest(); //응답이 올때까지 대기하기...

        if (a_www.error == null) //에러가 나지 않았을 때 동작
        {
            Debug.Log("UpDateSuccess~");
        }
        else
        {
            Debug.Log(a_www.error);
        }
    }

    public IEnumerator RefreshScoreCo()
    {
        WWWForm form = new WWWForm();
        form.AddField("My_ID", GlobalValue.Unique_ID,
                                        System.Text.Encoding.UTF8);
        form.AddField("Win_Count", GlobalValue.WinCount);
        form.AddField("Lose_Count", GlobalValue.LoseCount);

        UnityWebRequest a_www = UnityWebRequest.Post(RefreshScoreUrl, form);
        yield return a_www.SendWebRequest(); //응답이 올때까지 대기하기...

        if (a_www.error == null)
        {
            System.Text.Encoding enc = System.Text.Encoding.UTF8;
            string sz = enc.GetString(a_www.downloadHandler.data);

            if (sz.Contains("Refresh-Success!!") == false)
            {
                yield break;
            }

            if (sz.Contains("{\"") == false)
            {
                yield break;
            }

            string a_GetStr = sz.Substring(sz.IndexOf("{\""));

            var N = JSON.Parse(a_GetStr);
            if (N == null)
                yield break;

            if (N["wincount"] != null)
                GlobalValue.WinCount = N["wincount"].AsInt;

            if (N["losecount"] != null)
                GlobalValue.LoseCount = N["losecount"].AsInt;

            PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene("Lobby");
        }
        else
        {
            Debug.Log(a_www.error);
        }
    }
}
