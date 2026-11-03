using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 快速存 / 读档按钮，挂在虚拟按钮下面，跟随虚拟按钮一起显示和隐藏。
/// 支持多个档位：左右箭头夹在存/读档两侧用于切换，顶部 tab 只展示各档状态。
/// </summary>
public class GameQuickSaveUI : MonoBehaviour
{
    static GameQuickSaveUI instance;

    Button saveButton;
    Button loadButton;

    Button prevButton;
    Button nextButton;

    Image[] tabBgs;

    int currentSlot = 0;

    // 缓存当前档位是否有存档，避免每帧读文件
    bool currentHasSave = false;
    bool dirty = true;
    bool wasVisible = false;

    // 存档按钮正常态颜色（标题画面置灰时需要还原）
    Color saveBgNormalColor;
    Color saveTextNormalColor = Color.white;

    static Material uiMaterial = null;

    // 屏幕可用高度约 462（Canvas 参考 800 宽、按宽度匹配），
    // 按钮收窄到 68 并右对齐 A 按钮右边缘（-39.5），避开左侧的 ButtonC
    const float BTN_X = -73.5f;
    const float BTN_W = 68.0f;
    const float BTN_H = 40.0f;
    const float SAVE_Y = 334.0f;
    const float LOAD_Y = 288.0f;

    // 顶部档位圆点：3 个 15 直径 + 2 个 9 间距
    const float TAB_SIZE = 15.0f;
    const float TAB_GAP = 9.0f;
    const float TAB_Y = 374.0f;

    // 左右切换：圆形按钮夹在存档两侧
    const float ARROW_W = 34.0f;
    const float ARROW_H = 34.0f;
    const float ARROW_GAP = 3.0f;
    const float ARROW_CENTER_Y = 334.0f;

    static Sprite roundedSprite = null;
    static Sprite dotSprite = null;
    static Sprite arrowLeftSprite = null;
    static Sprite arrowRightSprite = null;

    /// <summary>在虚拟按钮的父节点下创建（已创建过就只做刷新）</summary>
    public static void attach( Transform parent )
    {
        if ( parent == null )
        {
            return;
        }

        if ( instance != null )
        {
            if ( instance.transform.parent != parent )
            {
                instance.transform.SetParent( parent , false );
            }

            instance.updateVisible();
            return;
        }

        // 旧版借用槽位时若被打断，这里把原档还回去
        GameQuickSave.recoverLegacySlot();

        GameObject go = new GameObject( "QuickSave" , typeof( RectTransform ) );

        go.transform.SetParent( parent , false );

        // 与虚拟按钮共用同一锚点空间（右下角），便于和 A 按钮对齐
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2( 1.0f , 0.0f );
        rt.anchorMax = new Vector2( 1.0f , 0.0f );
        rt.pivot = new Vector2( 0.5f , 0.5f );
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        instance = go.AddComponent<GameQuickSaveUI>();
        instance.init();
    }

    void init()
    {
        // 顶部 tab：只展示 3 个档位的状态，不响应点击
        int count = GameQuickSave.SLOT_COUNT;

        tabBgs = new Image[ count ];

        float tabStep = TAB_SIZE + TAB_GAP;
        float tabFirstX = BTN_X - tabStep * ( count - 1 ) * 0.5f;

        for ( int i = 0 ; i < count ; i++ )
        {
            tabBgs[ i ] = createTab( i , new Vector2( tabFirstX + tabStep * i , TAB_Y ) );
        }

        // 左右箭头夹在存/读档两侧
        float arrowOffset = BTN_W * 0.5f + ARROW_GAP + ARROW_W * 0.5f;

        prevButton = createSmallButton( "prev" , getArrowSprite( false ) ,
            new Vector2( BTN_X - arrowOffset , ARROW_CENTER_Y ) ,
            new Vector2( ARROW_W , ARROW_H ) , stepPrev );

        nextButton = createSmallButton( "next" , getArrowSprite( true ) ,
            new Vector2( BTN_X + arrowOffset , ARROW_CENTER_Y ) ,
            new Vector2( ARROW_W , ARROW_H ) , stepNext );

        saveButton = createButton( "save" , "存 档" , new Vector2( BTN_X , SAVE_Y ) , onSave );
        loadButton = createButton( "load" , "读 档" , new Vector2( BTN_X , LOAD_Y ) , onLoad );

        // 记录存档按钮正常态颜色，标题画面禁用时需要还原
        Image saveBg = saveButton.GetComponent<Image>();
        if ( saveBg != null )
        {
            saveBgNormalColor = saveBg.color;
        }

        refresh();
    }

    void stepPrev()
    {
        stepSlot( -1 );
    }

    void stepNext()
    {
        stepSlot( 1 );
    }

    /// <summary>循环切换档位</summary>
    void stepSlot( int delta )
    {
        int count = GameQuickSave.SLOT_COUNT;

        currentSlot = ( currentSlot + delta + count ) % count;

        updateSlotStates();

        Debug.Log( "[QuickSave] 选中档位 " + ( currentSlot + 1 ) );
    }

    public static void refresh()
    {
        if ( instance != null )
        {
            instance.updateVisible();
        }
    }

    void Update()
    {
        updateVisible();
    }

    void updateVisible()
    {
        // 标题画面也展示快速存读档：读档可用，存档按钮置灰禁用防误触
        bool isTitle = GameSceneManager.instance.SceneType == GameSceneType.Title;

        // 「今况记入 / 前历再续」界面打开时隐藏，避免压住存档槽位
        bool slOpen = ( GameSLUI.instance != null && GameSLUI.instance.IsShow );

        bool visible = !slOpen;

        // 每次变为可见时重新读一次档位状态，平时不重复读文件
        if ( visible && !wasVisible )
        {
            dirty = true;
        }

        wasVisible = visible;

        if ( saveButton != null )
        {
            saveButton.gameObject.SetActive( visible );

            // 标题画面禁用存档按钮：置灰 + 不可交互
            saveButton.interactable = !isTitle;

            Image bg = saveButton.GetComponent<Image>();

            if ( bg != null )
            {
                bg.color = isTitle
                    ? new Color( 0.20f , 0.20f , 0.22f , 0.7f )
                    : saveBgNormalColor;
            }

            Text t = saveButton.transform.Find( "text" ).GetComponent<Text>();

            if ( t != null )
            {
                t.color = isTitle
                    ? new Color( 0.55f , 0.55f , 0.58f , 0.7f )
                    : saveTextNormalColor;
            }
        }

        if ( loadButton != null )
        {
            loadButton.gameObject.SetActive( visible && currentHasSave );
        }

        if ( tabBgs != null )
        {
            for ( int i = 0 ; i < tabBgs.Length ; i++ )
            {
                if ( tabBgs[ i ] != null )
                {
                    tabBgs[ i ].gameObject.SetActive( visible );
                }
            }
        }

        if ( prevButton != null )
        {
            prevButton.gameObject.SetActive( visible );
        }

        if ( nextButton != null )
        {
            nextButton.gameObject.SetActive( visible );
        }

        if ( visible && dirty )
        {
            dirty = false;

            updateSlotStates();
        }
    }

    /// <summary>刷新顶部 tab：当前档位亮蓝，有档中蓝，无档深灰</summary>
    void updateSlotStates()
    {
        currentHasSave = GameQuickSave.HasSave( currentSlot );

        if ( tabBgs != null )
        {
            for ( int i = 0 ; i < tabBgs.Length ; i++ )
            {
                Image bg = tabBgs[ i ];

                if ( bg == null )
                {
                    continue;
                }

                bool hasSave = GameQuickSave.HasSave( i );
                bool selected = ( i == currentSlot );

                if ( selected )
                {
                    bg.color = new Color( 0.35f , 0.65f , 1.0f , 0.95f );
                }
                else if ( hasSave )
                {
                    bg.color = new Color( 0.25f , 0.5f , 0.9f , 0.55f );
                }
                else
                {
                    bg.color = new Color( 1.0f , 1.0f , 1.0f , 0.22f );
                }
            }
        }

        // 读档按钮按当前档位是否有档决定显示
        if ( loadButton != null )
        {
            loadButton.gameObject.SetActive( currentHasSave );
        }
    }

    void onSave()
    {
        // 标题画面没有游戏状态可存，存档按钮已置灰禁用，这里再兜底
        if ( GameSceneManager.instance.SceneType == GameSceneType.Title )
        {
            return;
        }

        bool ok = GameQuickSave.save( currentSlot );

        updateSlotStates();

        Text t = saveButton.transform.Find( "text" ).GetComponent<Text>();

        if ( t != null )
        {
            StartCoroutine( flash( t , ok ? ( "档" + ( currentSlot + 1 ) + " 已存" ) : "存档失败" ) );
        }
    }

    void onLoad()
    {
        GameQuickSave.load( currentSlot );
    }

    System.Collections.IEnumerator flash( Text t , string msg )
    {
        string old = t.text;

        t.text = msg;

        yield return new WaitForSecondsRealtime( 0.8f );

        t.text = old;
    }

    Button createButton( string name , string label , Vector2 pos , UnityEngine.Events.UnityAction onClick )
    {
        GameObject go = new GameObject( name , typeof( RectTransform ) );

        go.transform.SetParent( transform , false );

        RectTransform rt = go.GetComponent<RectTransform>();

        rt.anchorMin = new Vector2( 1.0f , 0.0f );
        rt.anchorMax = new Vector2( 1.0f , 0.0f );
        rt.pivot = new Vector2( 0.5f , 0.5f );
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2( BTN_W , BTN_H );

        Image bg = go.AddComponent<Image>();

        // 真机上 UI 默认材质可能为空，不显式指定会画成白块
        bg.material = getUIMaterial();
        bg.sprite = getRoundedSprite();
        bg.type = Image.Type.Sliced;
        bg.color = new Color( 0.09f , 0.12f , 0.18f , 0.88f );
        bg.raycastTarget = true;

        // 淡蓝描边，让按钮在杂乱的地图背景上更清楚
        Outline outline = go.AddComponent<Outline>();

        outline.effectColor = new Color( 0.45f , 0.68f , 1.0f , 0.5f );
        outline.effectDistance = new Vector2( 1.0f , -1.0f );
        outline.useGraphicAlpha = true;

        Button btn = go.AddComponent<Button>();

        btn.targetGraphic = bg;
        btn.transition = Selectable.Transition.None;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener( onClick );

        GameObject textObj = new GameObject( "text" , typeof( RectTransform ) );

        textObj.transform.SetParent( go.transform , false );

        RectTransform trt = textObj.GetComponent<RectTransform>();

        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        Text txt = textObj.AddComponent<Text>();

        txt.font = Resources.Load<Font>( "Fonts/GameFont" );
        txt.fontSize = 20;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = label;
        txt.raycastTarget = false;

        return btn;
    }

    /// <summary>左右箭头按钮</summary>
    Button createSmallButton( string name , Sprite iconSprite , Vector2 pos , Vector2 size , UnityEngine.Events.UnityAction onClick )
    {
        GameObject go = new GameObject( name , typeof( RectTransform ) );

        go.transform.SetParent( transform , false );

        RectTransform rt = go.GetComponent<RectTransform>();

        rt.anchorMin = new Vector2( 1.0f , 0.0f );
        rt.anchorMax = new Vector2( 1.0f , 0.0f );
        rt.pivot = new Vector2( 0.5f , 0.5f );
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image bg = go.AddComponent<Image>();

        bg.material = getUIMaterial();
        bg.sprite = getDotSprite();
        bg.type = Image.Type.Simple;
        bg.color = new Color( 0.13f , 0.18f , 0.27f , 0.92f );
        bg.raycastTarget = true;

        Outline outline = go.AddComponent<Outline>();

        outline.effectColor = new Color( 0.45f , 0.68f , 1.0f , 0.45f );
        outline.effectDistance = new Vector2( 1.0f , -1.0f );
        outline.useGraphicAlpha = true;

        Button btn = go.AddComponent<Button>();

        btn.targetGraphic = bg;
        btn.transition = Selectable.Transition.None;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        btn.onClick.AddListener( onClick );

        GameObject iconObj = new GameObject( "icon" , typeof( RectTransform ) );

        iconObj.transform.SetParent( go.transform , false );

        RectTransform irt = iconObj.GetComponent<RectTransform>();

        irt.anchorMin = new Vector2( 0.5f , 0.5f );
        irt.anchorMax = new Vector2( 0.5f , 0.5f );
        irt.pivot = new Vector2( 0.5f , 0.5f );
        // 折线箭头的视觉重心偏上，往下压 1 单位看起来才居中
        irt.anchoredPosition = new Vector2( 0.0f , -1.0f );
        irt.sizeDelta = new Vector2( 26.0f , 26.0f );

        Image icon = iconObj.AddComponent<Image>();

        icon.material = getUIMaterial();
        icon.sprite = iconSprite;
        icon.color = Color.white;
        icon.raycastTarget = false;

        return btn;
    }

    /// <summary>顶部档位圆点：纯展示，不接收点击</summary>
    Image createTab( int index , Vector2 pos )
    {
        GameObject go = new GameObject( "dot" + index , typeof( RectTransform ) );

        go.transform.SetParent( transform , false );

        RectTransform rt = go.GetComponent<RectTransform>();

        rt.anchorMin = new Vector2( 1.0f , 0.0f );
        rt.anchorMax = new Vector2( 1.0f , 0.0f );
        rt.pivot = new Vector2( 0.5f , 0.5f );
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2( TAB_SIZE , TAB_SIZE );

        Image bg = go.AddComponent<Image>();

        bg.material = getUIMaterial();
        bg.sprite = getDotSprite();
        bg.type = Image.Type.Simple;
        bg.color = new Color( 0.0f , 0.0f , 0.0f , 0.55f );
        bg.raycastTarget = false;

        return bg;
    }

    /// <summary>九宫格圆角矩形，用于存/读档按钮</summary>
    static Sprite getRoundedSprite()
    {
        if ( roundedSprite == null )
        {
            roundedSprite = createSprite( 64 , 10 );
        }

        return roundedSprite;
    }

    /// <summary>实心圆，用于档位圆点 / 圆形按钮</summary>
    static Sprite getDotSprite()
    {
        if ( dotSprite == null )
        {
            dotSprite = createSprite( 32 , 16 );
        }

        return dotSprite;
    }

    /// <summary>细线箭头图标（不依赖字体字形）</summary>
    static Sprite getArrowSprite( bool right )
    {
        if ( right ? arrowRightSprite != null : arrowLeftSprite != null )
            return right ? arrowRightSprite : arrowLeftSprite;

        int size = 32;

        Texture2D tex = new Texture2D( size , size , TextureFormat.RGBA32 , false );

        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[ size * size ];

        for ( int y = 0 ; y < size ; y++ )
        {
            for ( int x = 0 ; x < size ; x++ )
            {
                float hit = 0.0f;

                // 4×4 超采样，边缘平滑
                for ( int sy = 0 ; sy < 4 ; sy++ )
                {
                    for ( int sx = 0 ; sx < 4 ; sx++ )
                    {
                        float nx = ( x + ( sx + 0.5f ) / 4.0f ) / size;
                        float ny = ( y + ( sy + 0.5f ) / 4.0f ) / size;

                        if ( insideChevron( nx , ny , right ) )
                        {
                            hit += 1.0f;
                        }
                    }
                }

                pixels[ y * size + x ] = new Color32( 255 , 255 , 255 , (byte)( hit / 16.0f * 255.0f ) );
            }
        }

        tex.SetPixels32( pixels );
        tex.Apply();

        Sprite sprite = Sprite.Create( tex , new Rect( 0 , 0 , size , size ) , new Vector2( 0.5f , 0.5f ) , 100.0f , 0 ,
            SpriteMeshType.FullRect );

        if ( right )
        {
            arrowRightSprite = sprite;
        }
        else
        {
            arrowLeftSprite = sprite;
        }

        return sprite;
    }

    /// <summary>折线箭头（两条线段），right=false 时镜像成向左</summary>
    static bool insideChevron( float nx , float ny , bool right )
    {
        float cx = right ? nx : 1.0f - nx;

        return segmentDistance( cx , ny , 0.34f , 0.24f , 0.62f , 0.50f ) <= 0.09f ||
               segmentDistance( cx , ny , 0.62f , 0.50f , 0.34f , 0.76f ) <= 0.09f;
    }

    static float segmentDistance( float px , float py , float ax , float ay , float bx , float by )
    {
        float vx = bx - ax;
        float vy = by - ay;
        float wx = px - ax;
        float wy = py - ay;

        float len2 = vx * vx + vy * vy;
        float t = len2 > 0.0f ? Mathf.Clamp01( ( wx * vx + wy * vy ) / len2 ) : 0.0f;

        float dx = wx - vx * t;
        float dy = wy - vy * t;

        return Mathf.Sqrt( dx * dx + dy * dy );
    }

    /// <summary>运行时生成圆角（radius = size/2 时为正圆）的抗锯齿 sprite</summary>
    static Sprite createSprite( int size , int radius )
    {
        Texture2D tex = new Texture2D( size , size , TextureFormat.RGBA32 , false );

        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[ size * size ];

        float r = radius;

        for ( int y = 0 ; y < size ; y++ )
        {
            for ( int x = 0 ; x < size ; x++ )
            {
                float dx = Mathf.Max( r - ( x + 0.5f ) , ( x + 0.5f ) - ( size - r ) );
                float dy = Mathf.Max( r - ( y + 0.5f ) , ( y + 0.5f ) - ( size - r ) );

                dx = Mathf.Max( 0.0f , dx );
                dy = Mathf.Max( 0.0f , dy );

                float dist = Mathf.Sqrt( dx * dx + dy * dy ) - r;

                // 边缘 1 像素内做抗锯齿
                float alpha = Mathf.Clamp01( 0.5f - dist );

                pixels[ y * size + x ] = new Color32( 255 , 255 , 255 , (byte)( alpha * 255.0f ) );
            }
        }

        tex.SetPixels32( pixels );
        tex.Apply();

        // 正圆（radius = size/2）不需要九宫格边框
        Vector4 border = ( radius * 2 >= size )
            ? Vector4.zero
            : new Vector4( r , r , r , r );

        return Sprite.Create( tex , new Rect( 0 , 0 , size , size ) , new Vector2( 0.5f , 0.5f ) , 100.0f , 0 ,
            SpriteMeshType.FullRect , border );
    }

    static Material getUIMaterial()
    {
        if ( uiMaterial != null )
        {
            return uiMaterial;
        }

        uiMaterial = Graphic.defaultGraphicMaterial;

        if ( uiMaterial == null )
        {
            Shader shader = Shader.Find( "UI/Default" );

            if ( shader != null )
            {
                uiMaterial = new Material( shader );
            }
        }

        return uiMaterial;
    }
}
