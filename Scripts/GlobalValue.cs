using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum CharType
{
    pawn,
    rook,
    horse,
    bishop,
    queen,
    king
}

public class CharInfo
{

}

public class Pos
{
    public float tr_x = 0;
    public float tr_z = 0;
    public RawImage Img = null;
    public GameObject piece = null;
    public bool ismoved = false;
    public int charside = 0;
    public bool enpassant = false;
}

public class GlobalValue
{
    public static string Unique_ID = "";
    public static string Unique_EM = "";
    public static int WinCount = 0;
    public static int LoseCount = 0;
    public static bool MySide = true;   //true = White, false = Black
    public static int MySideNum = 0;    //1 = White, -1 = Black
    public static bool Host = false;
    public static bool isLogined = false;

    public static string Opponent_ID = "";
}
