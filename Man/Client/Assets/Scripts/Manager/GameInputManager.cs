﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;





public enum GameInputCode
{
    Up = 0,
    Down,
    Left,
    Right,

    Up1,
    Down1,

    Confirm,
    Cancel,

    Info,
    Debug,

    Count
}

public class GameInputManager : MonoBehaviour
{
    static bool[] lastInputAxisState = new bool[ (int)GameInputCode.Count ];

    // 返回该输入对应的自定义手柄按键索引，-1 表示沿用默认映射
    static int getPadButton( GameSetting setting , GameInputCode c )
    {
        switch ( c )
        {
            case GameInputCode.Up:
                return setting.padUp;
            case GameInputCode.Down:
                return setting.padDown;
            case GameInputCode.Left:
                return setting.padLeft;
            case GameInputCode.Right:
                return setting.padRight;
            case GameInputCode.Confirm:
                return setting.padConfirm;
            case GameInputCode.Cancel:
                return setting.padCancel;
        }

        return -1;
    }

    public static bool getKey( GameInputCode c )
    {
        // 虚拟控制器与物理手柄/键盘同时生效
        GameTouchManager touch = GameTouchManager.instance;

        if ( touch != null && touch.IsShow && touch.getKey( c ) )
        {
            return true;
        }

        GameSetting setting = GameSetting.instance;

        if ( setting != null && !setting.gamepad )
        {
            return false;
        }

        // 自定义手柄按键映射（在设定帮助里录制）
        if ( setting != null )
        {
            int pad = getPadButton( setting , c );

            if ( pad >= 0 && Input.GetKey( KeyCode.JoystickButton0 + pad ) )
            {
                return true;
            }
        }

        switch ( c )
        {
            case GameInputCode.Info:
                {
                    return Input.GetAxis( "Info" ) > 0.1f;
                }
            case GameInputCode.Up1:
                {
                    return Input.GetAxis( "PageUp" ) > 0.1f;
                }
            case GameInputCode.Down1:
                {
                    return Input.GetAxis( "PageDown" ) > 0.1f;
                }
            case GameInputCode.Up:
                {
                    return Input.GetAxis( "Vertical" ) > 0.1f;
                }
            case GameInputCode.Down:
                {
                    return Input.GetAxis( "Vertical" ) < -0.1f;
                }
            case GameInputCode.Left:
                {
                    return Input.GetAxis( "Horizontal" ) < -0.1f;
                }
            case GameInputCode.Right:
                {
                    return Input.GetAxis( "Horizontal" ) > 0.1f;
                }
            case GameInputCode.Confirm:
                {
                    return Input.GetAxis( "Submit" ) > 0.1f;
                }
            case GameInputCode.Cancel:
                {
                    return Input.GetAxis( "Cancel" ) > 0.1f;
                }
        }

        return false;
    }

    public static bool getKeyDown( GameInputCode c )
    {
        // 虚拟控制器与物理手柄/键盘同时生效
        GameTouchManager touch = GameTouchManager.instance;

        if ( touch != null && touch.IsShow && touch.getKeyDown( c ) )
        {
            return true;
        }

        GameSetting setting = GameSetting.instance;

        if ( setting != null && !setting.gamepad )
        {
            return false;
        }

        // 自定义手柄按键映射（在设定帮助里录制）
        if ( setting != null )
        {
            int pad = getPadButton( setting , c );

            if ( pad >= 0 && Input.GetKeyDown( KeyCode.JoystickButton0 + pad ) )
            {
                return true;
            }
        }

        switch ( c )
        {
            case GameInputCode.Debug:
                {
                    bool b = Input.GetAxis( "Debug" ) > 0.1f;

                    if ( b && lastInputAxisState[ (int)c ] )
                    {
                        return false;
                    }

                    lastInputAxisState[ (int)c ] = b;

                    return b;
                }
            case GameInputCode.Info:
                {
                    bool b = Input.GetAxis( "Info" ) > 0.1f;

                    if ( b && lastInputAxisState[ (int)c ] )
                    {
                        return false;
                    }

                    lastInputAxisState[ (int)c ] = b;

                    return b;
                }
            case GameInputCode.Up1:
                {
                    bool b = Input.GetAxis( "PageUp" ) > 0.1f;

                    if ( b && lastInputAxisState[ (int)c ] )
                    {
                        return false;
                    }

                    lastInputAxisState[ (int)c ] = b;

                    return b;
                }
            case GameInputCode.Down1:
                {
                    bool b = Input.GetAxis( "PageDown" ) > 0.1f;

                    if ( b && lastInputAxisState[ (int)c ] )
                    {
                        return false;
                    }

                    lastInputAxisState[ (int)c ] = b;

                    return b;
                }
            case GameInputCode.Up:
                {
                    bool b = Input.GetAxis( "Vertical" ) > 0.1f;

                    if ( b && lastInputAxisState[ (int)c ] )
                    {
                        return false;
                    }

                    lastInputAxisState[ (int)c ] = b;

                    return b;
                }
            case GameInputCode.Down:
                {
                    bool b = Input.GetAxis( "Vertical" ) < -0.1f;

                    if ( b && lastInputAxisState[ (int)c ] )
                    {
                        return false;
                    }

                    lastInputAxisState[ (int)c ] = b;

                    return b;
                }
            case GameInputCode.Left:
                {
                    bool b = Input.GetAxis( "Horizontal" ) < -0.1f;

                    if ( b && lastInputAxisState[ (int)c ] )
                    {
                        return false;
                    }

                    lastInputAxisState[ (int)c ] = b;

                    return b;
                }
            case GameInputCode.Right:
                {
                    bool b = Input.GetAxis( "Horizontal" ) > 0.1f;

                    if ( b && lastInputAxisState[ (int)c ] )
                    {
                        return false;
                    }

                    lastInputAxisState[ (int)c ] = b;

                    return b;
                }
            case GameInputCode.Confirm:
                {
                    bool b = Input.GetAxis( "Submit" ) > 0.1f;

                    if ( b && lastInputAxisState[ (int)c ] )
                    {
                        return false;
                    }

                    lastInputAxisState[ (int)c ] = b;

                    return b;
                }
            case GameInputCode.Cancel:
                {
                    bool b = Input.GetAxis( "Cancel" ) > 0.1f;

                    if ( b && lastInputAxisState[ (int)c ] )
                    {
                        return false;
                    }

                    lastInputAxisState[ (int)c ] = b;

                    return b;
                }
        }

        return false;
    }

}

