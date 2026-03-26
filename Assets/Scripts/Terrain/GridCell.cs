using UnityEngine;

namespace Game.Terrain
{
    // 地形の属性
    public enum ElementType
    {

    }

    // エディタの変更を保存する
    [System.Serializable]   
    public struct GridCell
    {
        public bool solid;                  // 地形が存在するかのフラグ
        public bool isStatic;               // 固定地形フラグ (物理演算によって動かない)
        public ElementType elementType;     // 地形の属性
        public float durability;            // 耐久値
        public float mass;                  // 質量
    }
}
