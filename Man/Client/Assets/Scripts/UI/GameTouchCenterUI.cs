﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameTouchCenterUI : GameUI<GameTouchCenterUI>
{
    public void showUI()
    {
#if ( UNITY_ANDROID || UNITY_IOS )

        if ( !GameSetting.instance.virtualPad )
        {
            return;
        }

        if ( GameTouchLeftUI.instance.IsShow || 
            GameTouchLeftUI.instance.IsFadeIn )
        {
            return;
        }

        GameTouchLeftUI.instance.showUI();
        GameTouchRightUI.instance.showUI();
        unShow();
#else
//         if ( GameTouchLeftUI.instance.IsShow ||
//             GameTouchLeftUI.instance.IsFadeIn )
//         {
//             return;
//         }
// 
//         GameTouchLeftUI.instance.showUI();
//         GameTouchRightUI.instance.showUI();
//         unShow();
#endif
    }

    public void unShowUI()
    {
#if ( UNITY_ANDROID || UNITY_IOS )

        if ( !GameSetting.instance.virtualPad )
        {
            return;
        }

        if ( !GameTouchLeftUI.instance.IsShow ||
            GameTouchLeftUI.instance.IsFadeOut )
        {
            return;
        }

        GameTouchLeftUI.instance.unShowFade();
        GameTouchRightUI.instance.unShowFade();
        show();
#else
//         if ( !GameTouchLeftUI.instance.IsShow ||
//             GameTouchLeftUI.instance.IsFadeOut )
//         {
//             return;
//         }
// 
//         GameTouchLeftUI.instance.unShowFade();
//         GameTouchRightUI.instance.unShowFade();
//         show();
#endif
    }

    // 应用「虚拟控制器」开关：关闭时隐藏屏幕按键，输入只走外接手柄或键盘
    public void applySetting()
    {
#if ( UNITY_ANDROID || UNITY_IOS )

        bool on = GameSetting.instance.virtualPad;

        GameTouchManager.instance.IsShow = on;

        if ( on )
        {
            showUI();
        }
        else
        {
            GameTouchLeftUI.instance.unShow();
            GameTouchRightUI.instance.unShow();
            unShow();
        }
#else
        GameTouchManager.instance.IsShow = GameSetting.instance.virtualPad;
#endif
    }
}
