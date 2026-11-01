﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameTouchRightUI : GameUI<GameTouchRightUI>
{
    public void showUI()
    {
        bool b = GameSceneManager.instance.SceneType == GameSceneType.Camp ||
            GameSceneManager.instance.SceneType == GameSceneType.Rpg;

        transform.Find( "ButtonC" ).gameObject.SetActive( b );
        transform.Find( "ButtonD" ).gameObject.SetActive( b );

        // 快速存/读档按钮跟着虚拟按钮一起出现
        GameQuickSaveUI.attach( transform );

        show();
        showFade();

        transform.localScale = new Vector3( GameSetting.instance.touchScale , GameSetting.instance.touchScale , GameSetting.instance.touchScale );
    }

}

