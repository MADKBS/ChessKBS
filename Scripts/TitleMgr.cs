using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using UnityEngine.UI;
using System.Globalization;
using System.Text.RegularExpressions;
using System;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class TitleMgr : MonoBehaviour
{
    [Header("Sel")]
    public GameObject SelObj;
    public Button Go_Login_Btn;
    public Button Go_Create_Btn;

    [Header("Login")]
    public GameObject LoginObj;
    public InputField Login_ID_IFd;
    public InputField Login_PW_IFd;
    public Button Login_Btn;
    public Button Login_Cancel_Btn;

    [Header("Create")]
    public GameObject CreateObj;
    public InputField Create_ID_IFd;
    public InputField Create_PW_IFd;
    public InputField Create_EM_IFd;
    public Button Create_Btn;
    public Button Create_Cancel_Btn;

    [Header("Normal")]
    public Text MessageTxt;
    float ShowMsTimer = 0.0f;

    bool invalidEmailType = false;
    bool isValidFormat = false;

    string LoginUrl;
    string CreateUrl;

    // Start is called before the first frame update
    void Start()
    {
        Go_Login_Btn.onClick.AddListener(GoLoginBtn);
        Go_Create_Btn.onClick.AddListener(GoCreateBtn);
        Login_Btn.onClick.AddListener(LoginBtn);
        Login_Cancel_Btn.onClick.AddListener(LoginCancelBtn);
        Create_Btn.onClick.AddListener(CreateBtn);
        Create_Cancel_Btn.onClick.AddListener(CreateCancelBtn);

        LoginUrl = "http://cholong1993.dothome.co.kr/Chess/Login.php";
        CreateUrl = "http://cholong1993.dothome.co.kr/Chess/CreateAccount.php";
    }

    // Update is called once per frame
    void Update()
    {
        if (0.0f < ShowMsTimer)
        {
            ShowMsTimer -= Time.deltaTime;
            if (ShowMsTimer <= 0.0f)
            {
                MessageOnOff("", false);
            }
        }
    }

    public void GoLoginBtn()
    {
        SelObj.SetActive(false);
        LoginObj.SetActive(true);
    }

    public void GoCreateBtn()
    {
        SelObj.SetActive(false);
        CreateObj.SetActive(true);
    }

    public void CreateBtn()
    {
        string IDStr = Create_ID_IFd.text.Trim();
        string PWStr = Create_PW_IFd.text.Trim();
        string EMStr = Create_EM_IFd.text.Trim();

        if (IDStr == "" || PWStr == "" || EMStr == "")
        {
            MessageOnOff("ID, PW, Email 빈칸 없이 입력해 주셔야 합니다.");
            return;
        }

        if (!(IDStr.Length >= 3 && IDStr.Length <= 10))
        {
            MessageOnOff("ID는 3글자 이상 10글자 이하로 작성해 주세요.");
            return;
        }

        if (!(PWStr.Length >= 4 && PWStr.Length <= 15))
        {
            MessageOnOff("비밀번호는 4글자 이상 15글자 이하로 작성해 주세요.");
            return;
        }

        if (!CheckEmailAddress(EMStr))
        {
            MessageOnOff("Email 형식이 맞지 않습니다.");
            return;
        }

        StartCoroutine(CreateCo(IDStr, PWStr, EMStr));
    }

    IEnumerator CreateCo(string IDStr, string PWStr, string EMStr)
    {
        WWWForm form = new WWWForm();
        form.AddField("Input_user", IDStr, System.Text.Encoding.UTF8);
        form.AddField("Input_pass", PWStr);
        form.AddField("Input_email", EMStr, System.Text.Encoding.UTF8);

        UnityWebRequest a_www = UnityWebRequest.Post(CreateUrl, form);
        yield return a_www.SendWebRequest();

        if (a_www.error == null)
        {
            System.Text.Encoding enc = System.Text.Encoding.UTF8;
            string sz = enc.GetString(a_www.downloadHandler.data);
            if (sz.Contains("Create Success.") == true)
            {
                //MessageOnOff(sz);
                MessageOnOff("성공적으로 생성되었습니다.");
                Debug.Log(sz);
            }
            else if (sz.Contains("ID does exist.") == true)
            {
                MessageOnOff("중복된 ID가 존재합니다.");
            }
            else if (sz.Contains("Email does exist.") == true)
            {
                MessageOnOff("중복된 Email이 존재합니다.");
            }
        }
        else
        {
            Debug.Log(a_www.error);
        }
    }

    public void CreateCancelBtn()
    {
        Create_ID_IFd.text = "";
        Create_PW_IFd.text = "";
        Create_EM_IFd.text = "";

        SelObj.SetActive(true);
        CreateObj.SetActive(false);
    }

    public void LoginBtn()
    {
        string IDStr = Login_ID_IFd.text.Trim();
        string PWStr = Login_PW_IFd.text.Trim();

        if (IDStr == "" || PWStr == "")
        {
            MessageOnOff("ID, PW 빈칸 없이 입력해 주셔야 합니다.");
            return;
        }

        if (!(IDStr.Length >= 3 && IDStr.Length <= 10))
        {
            MessageOnOff("ID는 3글자 이상 10글자 이하로 작성해 주세요.");
            return;
        }

        if (!(PWStr.Length >= 4 && PWStr.Length <= 15))
        {
            MessageOnOff("비밀번호는 4글자 이상 15글자 이하로 작성해 주세요.");
            return;
        }

        StartCoroutine(LoginCo(IDStr, PWStr));
    }

    IEnumerator LoginCo(string IDStr, string PWStr)
    {
        WWWForm form = new WWWForm();
        form.AddField("Input_user", IDStr, System.Text.Encoding.UTF8);
        form.AddField("Input_pass", PWStr);

        UnityWebRequest a_www = UnityWebRequest.Post(LoginUrl, form);
        yield return a_www.SendWebRequest();

        if (a_www.error == null)
        {
            System.Text.Encoding enc = System.Text.Encoding.UTF8;
            string sz = enc.GetString(a_www.downloadHandler.data);

            if (sz.Contains("Login-Success!!") == false)
            {
                ErrorMsg(sz);
                yield break;
            }

            if (sz.Contains("{\"") == false)
            {
                ErrorMsg(sz);
                yield break;
            }

            GlobalValue.Unique_ID = IDStr;

            string a_GetStr = sz.Substring(sz.IndexOf("{\""));

            var N = JSON.Parse(a_GetStr);
            if (N == null)
                yield break;

            if (N["nick_name"] != null)
                GlobalValue.Unique_EM = N["nick_name"];

            if (N["wincount"] != null)
                GlobalValue.WinCount = N["wincount"].AsInt;

            if (N["losecount"] != null)
                GlobalValue.LoseCount = N["losecount"].AsInt;

            SceneManager.LoadScene("Lobby");
        }
        else
        {
            Debug.Log(a_www.error);
        }
    }

    public void LoginCancelBtn()
    {
        Login_ID_IFd.text = "";
        Login_PW_IFd.text = "";

        SelObj.SetActive(true);
        LoginObj.SetActive(false);
    }

    void MessageOnOff(string Mess = "", bool isOn = true)
    {
        if (isOn == true)
        {
            MessageTxt.text = Mess;
            MessageTxt.gameObject.SetActive(true);
            ShowMsTimer = 7.0f;
        }
        else
        {
            MessageTxt.text = "";
            MessageTxt.gameObject.SetActive(false);
        }
    }

    void ErrorMsg(string Str)
    {
        if (Str.Contains("ID does not exist.") == true)
        {
            MessageOnOff("ID가 존재하지 않습니다.");

        }
        else if (Str.Contains("Pass does not Match.") == true)
        {
            MessageOnOff("패스워드가 일치하지 않습니다.");
        }
        else if (Str.Contains("</head>") == true)
        {
            string GetStr = Str.Substring(Str.IndexOf("</head>") + 8);
            MessageOnOff(GetStr);
        }
        else
        {
            MessageOnOff(Str);
            Debug.Log(Str);
        }
    }

    private bool CheckEmailAddress(string EmailStr)
    {
        if (string.IsNullOrEmpty(EmailStr)) isValidFormat = false;

        EmailStr = Regex.Replace(EmailStr, @"(@)(.+)$", this.DomainMapper, RegexOptions.None);
        if (invalidEmailType) isValidFormat = false;

        // true 로 반환할 시, 올바른 이메일 포맷임.
        isValidFormat = Regex.IsMatch(EmailStr,
                      @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                      @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                      RegexOptions.IgnoreCase);
        return isValidFormat;
    }

    private string DomainMapper(Match match)
    {
        // IdnMapping class with default property values.
        IdnMapping idn = new IdnMapping();

        string domainName = match.Groups[2].Value;
        try
        {
            domainName = idn.GetAscii(domainName);
        }
        catch (ArgumentException)
        {
            invalidEmailType = true;
        }
        return match.Groups[1].Value + domainName;
    }
}
