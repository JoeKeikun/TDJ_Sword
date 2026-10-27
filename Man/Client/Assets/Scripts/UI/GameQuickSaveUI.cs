using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 快速存 / 读档按钮，挂在虚拟按钮下面，跟随虚拟按钮一起显示和隐藏。
/// 没有快速存档时不显示「读档」。
/// </summary>
public class GameQuickSaveUI : MonoBehaviour
{
    static GameQuickSaveUI instance;

    Button saveButton;
    Button loadButton;

    static Material uiMaterial = null;

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

        instance = go.AddComponent<GameQuickSaveUI>();
        instance.init();
    }

    void init()
    {
        saveButton = createButton( "save" , "存 档" , new Vector2( -160.0f , 400.0f ) , onSave );
        loadButton = createButton( "load" , "读 档" , new Vector2( -280.0f , 400.0f ) , onLoad );

        refresh();
    }

    public static void refresh()
    {
        if ( instance != null )
        {
            instance.updateVisible();
        }
    }

    void updateVisible()
    {
        // 标题界面用不到快速存读档
        bool inGame = GameSceneManager.instance.SceneType != GameSceneType.Title;

        gameObject.SetActive( inGame );

        if ( loadButton != null )
        {
            loadButton.gameObject.SetActive( GameQuickSave.HasSave );
        }
    }

    void onSave()
    {
        bool ok = GameQuickSave.save();

        updateVisible();

        Text t = saveButton.transform.Find( "text" ).GetComponent<Text>();

        if ( t != null )
        {
            StartCoroutine( flash( t , ok ? "已存档" : "存档失败" ) );
        }
    }

    void onLoad()
    {
        GameQuickSave.load();
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
        rt.sizeDelta = new Vector2( 108.0f , 50.0f );

        Image bg = go.AddComponent<Image>();

        // 真机上 UI 默认材质可能为空，不显式指定会画成白块
        bg.material = getUIMaterial();
        bg.color = new Color( 0.0f , 0.0f , 0.0f , 0.55f );
        bg.raycastTarget = true;

        Button btn = go.AddComponent<Button>();

        btn.targetGraphic = bg;
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
        txt.fontSize = 24;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = label;
        txt.raycastTarget = false;

        return btn;
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
