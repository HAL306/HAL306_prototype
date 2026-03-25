using UnityEngine;
using UnityEditor;
using Game.Terrain;
    
// TerrainContextクラスのInspectorをカスタムする
[CustomEditor(typeof(TerrainContext))]

/// <summary>
/// エディタを起動するためにContextのインスペクターを変える
/// </summary>
public class CellMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 開いたエディタウィンドウ
        TerrainEditorWindow window;

        // ターゲットとなるCellMapのインスタンスを取得
        TerrainContext map = (TerrainContext)target;

        GUILayout.Space(10); // 少し隙間を空ける

        // エディタ起動ボタン
        if (GUILayout.Button("専用エディタを開く", GUILayout.Height(30)))
        {
            // 別ウィンドウを起動し、編集対象のmapデータを渡す
            window = TerrainEditorWindow.OpenWindow(ref map);
        }
    }
}