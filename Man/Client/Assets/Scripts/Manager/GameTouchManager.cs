using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


class GameTouchManager : SingletonManager<GameTouchManager>
{
    bool[] isDonw = new bool[ (int)GameInputCode.Count ];
    bool[] isPress = new bool[ (int)GameInputCode.Count ];

    public bool IsShow { get; set; }
    public override void initSingleton()
    {
#if ( UNITY_ANDROID || UNITY_IOS )
        IsShow = true;
#else
        IsShow = false;
#endif
    }

    public bool getKeyDown( GameInputCode c )
    {
        bool b = isDonw[ (int)c ];
        isDonw[ (int)c ] = false;

        return b;
    }

    public bool getKey( GameInputCode c )
    {
        bool b = isPress[ (int)c ];
        isPress[ (int)c ] = false;

        return b;
    }

    public void onTouchUp()
    {
        for ( int i = 0 ; i < (int)GameInputCode.Count ; i++ )
        {
            isDonw[ i ] = false;
            isPress[ i ] = false;
        }
    }

    public void OnButtonADown()
    {
        isDonw[ (int)GameInputCode.Confirm ] = true;
        GameHapticFeedback.Play(GameHapticFeedback.HapticType.Medium);
    }

    public void OnButtonBDown()
    {
        isDonw[ (int)GameInputCode.Cancel ] = true;
        GameHapticFeedback.Play(GameHapticFeedback.HapticType.Light);
    }


    public void OnDownUp1()
    {
        isDonw[ (int)GameInputCode.Up1 ] = true;
        GameHapticFeedback.Play(GameHapticFeedback.HapticType.Selection);
    }
    public void OnDownDown1()
    {
        isDonw[ (int)GameInputCode.Down1 ] = true;
        GameHapticFeedback.Play(GameHapticFeedback.HapticType.Selection);
    }
    public void OnDownUp()
    {
        isDonw[ (int)GameInputCode.Up ] = true;
    }
    public void OnDownDown()
    {
        isDonw[ (int)GameInputCode.Down ] = true;
    }
    public void OnDownLeft()
    {
        isDonw[ (int)GameInputCode.Left ] = true;
    }
    public void OnDownRight()
    {
        isDonw[ (int)GameInputCode.Right ] = true;
    }


    public void OnPressUp1()
    {
        isPress[ (int)GameInputCode.Up1 ] = true;
    }
    public void OnPressDown1()
    {
        isPress[ (int)GameInputCode.Down1 ] = true;
    }
    public void OnPressUp()
    {
        isPress[ (int)GameInputCode.Up ] = true;
    }
    public void OnPressDown()
    {
        isPress[ (int)GameInputCode.Down ] = true;
    }
    public void OnPressLeft()
    {
        isPress[ (int)GameInputCode.Left ] = true;
    }
    public void OnPressRight()
    {
        isPress[ (int)GameInputCode.Right ] = true;
    }

    float checkPadTime = 0.0f;

    void Update()
    {
        GameSetting setting = GameSetting.instance;

        if ( setting == null || setting.virtualPad )
        {
            checkPadTime = 0.0f;

            return;
        }

        checkPadTime += Time.deltaTime;

        if ( checkPadTime < 1.0f )
        {
            return;
        }

        checkPadTime = 0.0f;

        // 关闭虚拟控制器后如果没有任何外接手柄，自动恢复，避免无法操作
        if ( Input.GetJoystickNames().Length == 0 )
        {
            setting.virtualPad = true;

            setting.save();

            GameTouchCenterUI touch = GameTouchCenterUI.instance;

            if ( touch != null )
            {
                touch.applySetting();
            }
        }
    }

}

