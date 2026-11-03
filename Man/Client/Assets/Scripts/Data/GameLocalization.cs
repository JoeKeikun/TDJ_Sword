using UnityEngine;
using UnityEngine.UI;

class GameLocalization : MonoBehaviour
{
    public GameStringType type;

    void Start()
    {
        updateText();
    }

    public void updateText()
    {
        Text text = GetComponent<Text>();

        // 场景里保存的 type 是旧字符串表（76 条）的下标，旧表在 35/36 多出两条
        // 已删除的云存档文案，导致 37 之后的文案整体后移 2 位。
        // 代码里的字符串表已按枚举重建（68 条），这里做兼容换算，避免设置项等标签错位。
        int index = (int)type;

        if ( index >= 37 )
        {
            index -= 2;
        }

        text.text = GameStringData.instance.getString( (GameStringType)index );
    }

}
