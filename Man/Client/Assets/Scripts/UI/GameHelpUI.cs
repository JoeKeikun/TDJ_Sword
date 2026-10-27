using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameHelpUI : GameUI<GameHelpUI>
{
    Text[] text = new Text[ 16 ];
    int selection = 0;
    public int Selection { get { return selection; } }

    // 外链（0-4）、QQ 群（5）、5星好评（10）选项已移除
    const int RemovedEntryCount = 6;

    // 新增项：虚拟控制器、外接手柄、手柄按键设置、震动反馈
    const int EntryVirtualPad = 12;
    const int EntryGamepad = 13;
    const int EntryPadMap = 14;
    const int EntryHaptic = 15;

    // 面板自上而下的选择顺序
    static readonly int[] order = { EntryVirtualPad, EntryGamepad, EntryPadMap, EntryHaptic, 6, 7, 8, 9, 11 };

    Text setting00;
    Text setting10;
    Text setting2;

    Text virtualPadValue;
    Text gamepadValue;
    Text padMapValue;
    Text hapticValue;
    Text hint;

    // 手柄按键录制：0 空闲 / 1 确认 / 2 取消 / 3 上 / 4 下 / 5 左 / 6 右
    int captureStep = 0;
    float captureTime = 0.0f;

    // 临时提示的剩余显示时间
    float messageTime = 0.0f;

    public bool IsCapturing { get { return captureStep != 0; } }

    public override void initSingleton()
    {
        text[ 0 ] = transform.Find( "button0/text" ).GetComponent<Text>();
        text[ 1 ] = transform.Find( "button1/text" ).GetComponent<Text>();
        text[ 2 ] = transform.Find( "button2/text" ).GetComponent<Text>();
        text[ 3 ] = transform.Find( "button3/text" ).GetComponent<Text>();
        text[ 4 ] = transform.Find( "button4/text" ).GetComponent<Text>();
        text[ 5 ] = transform.Find( "button5/text" ).GetComponent<Text>();

        text[ 6 ] = transform.Find( "setting0/text" ).GetComponent<Text>();
        text[ 7 ] = transform.Find( "setting1/text" ).GetComponent<Text>();

        text[ 8 ] = transform.Find( "setting20" ).GetComponent<Text>();
        text[ 9 ] = transform.Find( "setting21" ).GetComponent<Text>();

        text[ 11 ] = transform.Find( "button7/text" ).GetComponent<Text>();

        setting00 = transform.Find( "setting00" ).GetComponent<Text>();
        setting10 = transform.Find( "setting10" ).GetComponent<Text>();

        setting2 = transform.Find( "setting2/text" ).GetComponent<Text>();

        for ( int i = 0 ; i < RemovedEntryCount ; i++ )
        {
            Transform button = transform.Find( "button" + i );

            if ( button != null )
            {
                button.gameObject.SetActive( false );
            }
        }

        hideButton( "button6" );

        text[ EntryVirtualPad ] = createRow( "虚拟控制器" , 50.0f , out virtualPadValue );
        text[ EntryGamepad ] = createRow( "外接手柄" , 36.0f , out gamepadValue );
        text[ EntryPadMap ] = createRow( "手柄按键" , 22.0f , out padMapValue );
        text[ EntryHaptic ] = createRow( "震动反馈" , 8.0f , out hapticValue );

        hint = createText( "Hint" , new Vector2( 0.0f , -65.0f ) , new Vector2( 132.0f , 20.0f ) );

        hint.transform.localScale = Vector3.one;
        hint.fontSize = 8;
        hint.horizontalOverflow = HorizontalWrapMode.Wrap;
        hint.verticalOverflow = VerticalWrapMode.Overflow;
        hint.alignment = TextAnchor.MiddleCenter;

        // 平时不显示文字提示，只在录制手柄按键时显示操作指引
        hint.gameObject.SetActive( false );

        layoutRows();
    }

    void hideButton( string name )
    {
        Transform button = transform.Find( name );

        if ( button != null )
        {
            button.gameObject.SetActive( false );
        }
    }

    // 重新排布各行的纵向位置，使去掉项后的内容在面板中居中
    void layoutRows()
    {
        setRowY( text[ 6 ].transform.parent , setting00.transform , -6.0f );
        setRowY( text[ 7 ].transform.parent , setting10.transform , -21.0f );

        setRowY( text[ 8 ].transform , null , -36.0f );
        setRowY( text[ 9 ].transform , null , -36.0f );
        setRowY( setting2.transform.parent , null , -36.0f );

        setRowY( text[ 11 ].transform.parent , null , -51.0f );
    }

    void setRowY( Transform row , Transform value , float y )
    {
        setY( row , y );

        if ( value != null )
        {
            setY( value , y );
        }
    }

    void setY( Transform trans , float y )
    {
        RectTransform rect = trans as RectTransform;

        if ( rect == null )
        {
            return;
        }

        Vector2 pos = rect.anchoredPosition;

        pos.y = y;

        rect.anchoredPosition = pos;
    }

    public override void onShow()
    {
        messageTime = 0.0f;

        updateSettingText();
        updateToggleText();
        updateHint();
    }

    public void selectFirst()
    {
        selection = order[ 0 ];

        updateText();
    }

    // dir: -1 上移 / +1 下移，按面板显示顺序循环
    public void move( int dir )
    {
        if ( captureStep != 0 )
        {
            return;
        }

        int index = Array.IndexOf( order , selection );

        if ( index < 0 )
        {
            index = 0;
        }

        index = ( index + dir + order.Length ) % order.Length;

        selection = order[ index ];

        updateText();
    }

    public void onSetting0()
    {
        if ( GameSetting.instance.mode == GameSetting.GameMode.Normal )
        {
            GameSetting.instance.mode = GameSetting.GameMode.Hard;
        }
        else
        {
            GameSetting.instance.mode = GameSetting.GameMode.Normal;
        }

        updateSettingText();

        GameSetting.instance.save();
    }

    public void onSetting1()
    {
        if ( GameSetting.instance.location == GameSetting.GameLocation.SimplifiedChinese )
        {
            GameSetting.instance.location = GameSetting.GameLocation.TraditionalChinese;
        }
        else
        {
            GameSetting.instance.location = GameSetting.GameLocation.SimplifiedChinese;
        }

        GameObject objCanvas = GameObject.Find( "Canvas" );
        GameLocalization[] d = objCanvas.GetComponentsInChildren<GameLocalization>();

        for ( int i = 0 ; i < d.Length ; i++ )
        {
            d[ i ].updateText();
        }

        updateSettingText();

        GameSetting.instance.save();
    }

    public void onSetting20()
    {
        GameSetting.instance.touchScale -= 0.1f;

        if ( GameSetting.instance.touchScale < 1.0f )
        {
            GameSetting.instance.touchScale = 1.0f;
        }

        GameTouchLeftUI.instance.transform.localScale = new Vector3( GameSetting.instance.touchScale , GameSetting.instance.touchScale , GameSetting.instance.touchScale );
        GameTouchRightUI.instance.transform.localScale = new Vector3( GameSetting.instance.touchScale , GameSetting.instance.touchScale , GameSetting.instance.touchScale );

        GameSetting.instance.save();
    }

    public void onSetting21()
    {
        GameSetting.instance.touchScale += 0.1f;

        if ( GameSetting.instance.touchScale > 1.5f )
        {
            GameSetting.instance.touchScale = 1.5f;
        }

        GameTouchLeftUI.instance.transform.localScale = new Vector3( GameSetting.instance.touchScale , GameSetting.instance.touchScale , GameSetting.instance.touchScale );
        GameTouchRightUI.instance.transform.localScale = new Vector3( GameSetting.instance.touchScale , GameSetting.instance.touchScale , GameSetting.instance.touchScale );

        GameSetting.instance.save();
    }

    public void onVirtualPad()
    {
        GameSetting setting = GameSetting.instance;

        // 没有外接手柄时不允许关闭，否则屏幕上将没有任何可操作输入
        if ( setting.virtualPad && Input.GetJoystickNames().Length == 0 )
        {
            showMessage( "未检测到外接手柄，保留虚拟控制器" );

            return;
        }

        setting.virtualPad = !setting.virtualPad;

        setting.save();

        GameTouchCenterUI.instance.applySetting();

        updateToggleText();
        updateHint();
    }

    public void onGamepad()
    {
        GameSetting.instance.gamepad = !GameSetting.instance.gamepad;

        GameSetting.instance.save();

        updateToggleText();
        updateHint();
    }

    public void onHaptic()
    {
        GameSetting setting = GameSetting.instance;

        setting.haptic = !setting.haptic;

        GameHapticFeedback.Enabled = setting.haptic;

        setting.save();

        if ( setting.haptic )
        {
            GameHapticFeedback.Play( GameHapticFeedback.HapticType.Light );
        }

        updateToggleText();
        updateHint();
    }

    public void onPadMap()
    {
        captureStep = 1;
        captureTime = 0.0f;

        hint.gameObject.SetActive( true );
        hint.text = "请按【确认】键…（8秒）";
    }

    protected override void onUpdate()
    {
        if ( captureStep == 0 )
        {
            tickMessage();

            return;
        }

        captureTime += Time.deltaTime;

        if ( captureTime > 8.0f )
        {
            finishCapture( false );

            return;
        }

        // 忽略刚进入录制瞬间按下的键（就是用来进入"手柄按键"项的确认键）
        if ( captureTime < 0.35f )
        {
            return;
        }

        if ( !Input.anyKeyDown )
        {
            return;
        }

        for ( int i = 0 ; i < 20 ; i++ )
        {
            if ( Input.GetKeyDown( KeyCode.JoystickButton0 + i ) )
            {
                recordPadButton( i );

                return;
            }
        }
    }

    void recordPadButton( int index )
    {
        GameSetting setting = GameSetting.instance;

        switch ( captureStep )
        {
            case 1:
                setting.padConfirm = index;
                captureStep = 2;
                hint.text = "请按【取消】键…（8秒）";
                break;
            case 2:
                setting.padCancel = index;
                captureStep = 3;
                hint.text = "请按【上】键…（8秒）";
                break;
            case 3:
                setting.padUp = index;
                captureStep = 4;
                hint.text = "请按【下】键…（8秒）";
                break;
            case 4:
                setting.padDown = index;
                captureStep = 5;
                hint.text = "请按【左】键…（8秒）";
                break;
            case 5:
                setting.padLeft = index;
                captureStep = 6;
                hint.text = "请按【右】键…（8秒）";
                break;
            case 6:
                setting.padRight = index;
                finishCapture( true );
                break;
        }

        captureTime = 0.0f;
    }

    void finishCapture( bool save )
    {
        captureStep = 0;
        captureTime = 0.0f;

        if ( save )
        {
            GameSetting.instance.save();
        }

        updateToggleText();
        updateHint();
    }

    public void updateSettingText()
    {
        if ( GameSetting.instance.mode == GameSetting.GameMode.Normal )
        {
            setting00.text = GameStringData.instance.getString( GameStringType.Setting01 );
        }
        else
        {
            setting00.text = GameStringData.instance.getString( GameStringType.Setting02 );
        }

        if ( GameSetting.instance.location == GameSetting.GameLocation.SimplifiedChinese )
        {
            setting10.text = GameStringData.instance.getString( GameStringType.Setting11 );
        }
        else
        {
            setting10.text = GameStringData.instance.getString( GameStringType.Setting12 );
        }
    }

    void updateToggleText()
    {
        GameSetting setting = GameSetting.instance;

        virtualPadValue.text = setting.virtualPad ? "开" : "关";
        gamepadValue.text = setting.gamepad ? "开" : "关";
        hapticValue.text = setting.haptic ? "开" : "关";
        padMapValue.text = ( setting.padConfirm >= 0 && setting.padCancel >= 0 &&
            setting.padUp >= 0 && setting.padDown >= 0 &&
            setting.padLeft >= 0 && setting.padRight >= 0 ) ? "已设置" : "默认";
    }

    // 短暂显示一条提示（平时提示行是隐藏的）
    void showMessage( string str )
    {
        if ( hint == null )
        {
            return;
        }

        hint.text = str;
        hint.gameObject.SetActive( true );

        messageTime = 2.0f;
    }

    void tickMessage()
    {
        if ( messageTime <= 0.0f )
        {
            return;
        }

        messageTime -= Time.deltaTime;

        if ( messageTime <= 0.0f )
        {
            updateHint();
        }
    }

    // 平时隐藏提示行，只在录制手柄按键时显示
    void updateHint()
    {
        if ( hint == null )
        {
            return;
        }

        if ( captureStep != 0 )
        {
            return;
        }

        hint.text = "";
        hint.gameObject.SetActive( false );
    }

    // 未选中：深褐色文字 + 浅色描边；选中：亮白文字 + 深色描边（字号与第一版保持一致，不再放大）
    void styleText( Text t , bool selected )
    {
        if ( t == null )
        {
            return;
        }

        if ( selected )
        {
            t.color = new Color( 1.0f , 0.98f , 0.85f , 1.0f );
        }
        else
        {
            t.color = new Color( 0.20f , 0.10f , 0.02f , 0.95f );
        }

        Outline outline = t.GetComponent<Outline>();

        if ( outline != null )
        {
            if ( selected )
            {
                outline.effectColor = new Color( 0.32f , 0.10f , 0.0f , 1.0f );
            }
            else
            {
                outline.effectColor = new Color( 1.0f , 0.95f , 0.75f , 0.6f );
            }

            outline.effectDistance = new Vector2( 1.0f , -1.0f );
        }
    }

    public void updateText()
    {
        for ( int i = 0 ; i < order.Length ; i++ )
        {
            styleText( text[ order[ i ] ] , selection == order[ i ] );
        }

        styleText( virtualPadValue , selection == EntryVirtualPad );
        styleText( gamepadValue , selection == EntryGamepad );
        styleText( hapticValue , selection == EntryHaptic );
        styleText( padMapValue , selection == EntryPadMap );

        styleText( setting00 , selection == 6 );
        styleText( setting10 , selection == 7 );
        styleText( setting2 , selection == 8 || selection == 9 );

        updateHint();
    }

    // 新建一行“标签 + 值（开/关）”
    Text createRow( string label , float y , out Text value )
    {
        Text labelText = createText( label , new Vector2( -31.7f , y ) , new Vector2( 64.0f , 20.0f ) );

        labelText.text = label;

        value = createText( label + "Value" , new Vector2( 26.0f , y ) , new Vector2( 88.0f , 30.0f ) );

        return labelText;
    }

    Text createText( string name , Vector2 pos , Vector2 size )
    {
        GameObject obj = new GameObject( name , typeof( RectTransform ) );

        RectTransform trans = obj.GetComponent<RectTransform>();

        trans.SetParent( transform , false );
        trans.anchorMin = new Vector2( 0.5f , 0.5f );
        trans.anchorMax = new Vector2( 0.5f , 0.5f );
        trans.pivot = new Vector2( 0.5f , 0.5f );
        trans.anchoredPosition = pos;
        trans.sizeDelta = size;
        trans.localScale = text[ 6 ].transform.localScale;

        Text t = obj.AddComponent<Text>();

        t.font = text[ 6 ].font;
        t.fontSize = text[ 6 ].fontSize;
        t.fontStyle = FontStyle.Normal;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.text = "";

        Outline outline = obj.AddComponent<Outline>();

        outline.effectDistance = new Vector2( 1.2f , -1.2f );

        return t;
    }

    public void onClick7()
    {
        unShow();
    }

}
