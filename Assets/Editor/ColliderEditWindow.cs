// CellMapWindow.cs
using Game.Terrain;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// セルのデータ定義（Cloneメソッドを追加）
[System.Serializable]
public class CellData
{
    public string cellName = "Empty";
    public int cellValue = 0;
    public Color cellColor = Color.gray;

    // パレットから配置する際に、データのコピー（別の実体）を作るための関数
    public CellData Clone()
    {
        return new CellData
        {
            cellName = this.cellName,
            cellValue = this.cellValue,
            cellColor = this.cellColor
        };
    }
}

public class ColliderEditWindow : EditorWindow
{
    // 編集対象のデータを保持する変数
    private TerrainContext targetMap;

    // 配置先の配列（キャンバス）
    public CellData[] gridCells = new CellData[0];
    private int gridWidth = 5;
    private int gridHeight = 5;

    // パレット用のリスト（選択肢）
    public List<CellData> paletteTemplates = new List<CellData>();

    // 現在選択されているパレットのインデックス (-1は未選択)
    private int selectedPaletteIndex = -1;

    private Vector2 gridScrollPosition;
    private Vector2 paletteScrollPosition;

    // インスペクターのボタンから呼ばれる起動メソッド
    public static void OpenWindow(TerrainContext map)
    {
        // ウィンドウを生成・取得
        ColliderEditWindow window = GetWindow<ColliderEditWindow>("Collider Editor");
        window.targetMap = map; // 編集対象をセット
        window.Show();
    }

    private void OnEnable()
    {
        // ウィンドウを開いた時に、テスト用のパレットデータをいくつか用意しておく
        if (paletteTemplates.Count == 0)
        {
            paletteTemplates.Add(new CellData { cellName = "Grass", cellValue = 1, cellColor = Color.green });
            paletteTemplates.Add(new CellData { cellName = "Water", cellValue = 2, cellColor = Color.blue });
            paletteTemplates.Add(new CellData { cellName = "Wall", cellValue = 3, cellColor = new Color(0.3f, 0.3f, 0.3f) });
            paletteTemplates.Add(new CellData { cellName = "Fire", cellValue = 4, cellColor = Color.red });
        }
        GenerateGrid();
    }

    // ウィンドウ内の描画処理
    private void OnGUI()
    {
        // 編集対象がロストした場合（シーンを切り替えた時など）のガード
        if (targetMap == null)
        {
            GUILayout.Label("編集対象のCellMapが見つかりません。");
            GUILayout.Label("インスペクターから再度開いてください。");
            return;
        }

        GUILayout.Label($"{targetMap.gameObject.name} を編集中", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // --- グリッドの描画 ---
        // ==========================================
        // 1. 設定エリア
        // ==========================================
        GUILayout.BeginHorizontal();
        gridWidth = EditorGUILayout.IntField("Width", gridWidth);
        gridHeight = EditorGUILayout.IntField("Height", gridHeight);
        if (GUILayout.Button("Reset Grid", GUILayout.Width(100)))
        {
            GenerateGrid();
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // ==========================================
        // 2. パレットエリア（リストから選ぶ）
        // ==========================================
        GUILayout.Label("パレット (クリックで選択)", EditorStyles.boldLabel);
        paletteScrollPosition = EditorGUILayout.BeginScrollView(paletteScrollPosition, GUILayout.Height(100));
        GUILayout.BeginHorizontal();

        for (int i = 0; i < paletteTemplates.Count; i++)
        {
            var template = paletteTemplates[i];

            // 選択中のものは背景色を黄色にして目立たせる
            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = (i == selectedPaletteIndex) ? Color.yellow : template.cellColor;

            // パレットのボタンを描画
            string buttonText = $"{template.cellName}\nVal: {template.cellValue}";
            if (GUILayout.Button(buttonText, GUILayout.Width(80), GUILayout.Height(60)))
            {
                selectedPaletteIndex = i; // クリックで選択
            }

            GUI.backgroundColor = defaultColor; // 色を元に戻す
        }

        GUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // ==========================================
        // 3. グリッドエリア（配列を編集するエリア）
        // ==========================================
        GUILayout.Label("キャンバス (クリックで配置)", EditorStyles.boldLabel);

        if (gridCells == null || gridCells.Length == 0) return;

        gridScrollPosition = EditorGUILayout.BeginScrollView(gridScrollPosition);

        for (int y = 0; y < gridHeight; y++)
        {
            GUILayout.BeginHorizontal();

            for (int x = 0; x < gridWidth; x++)
            {
                int index = y * gridWidth + x;
                if (index >= gridCells.Length) continue;

                var cell = gridCells[index];

                // セルの色をボタンの背景色に反映
                Color defaultBgColor = GUI.backgroundColor;
                GUI.backgroundColor = cell.cellColor;

                // グリッド上のボタン（クリックでパレットのデータを適用）
                if (GUILayout.Button(cell.cellName, GUILayout.Width(60), GUILayout.Height(60)))
                {
                    // パレットが選択されていれば、そのデータをクローンして上書きする
                    if (selectedPaletteIndex >= 0 && selectedPaletteIndex < paletteTemplates.Count)
                    {
                        gridCells[index] = paletteTemplates[selectedPaletteIndex].Clone();
                    }
                }

                GUI.backgroundColor = defaultBgColor;
            }

            GUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();




        EditorGUI.BeginChangeCheck();
        GUI.backgroundColor = Color.white;

        // 変更があった場合はセーブ対象としてマークする
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(targetMap);
        }
    }

    // 配列を空のセルで初期化する
    private void GenerateGrid()
    {
        gridCells = new CellData[gridWidth * gridHeight];
        for (int i = 0; i < gridCells.Length; i++)
        {
            gridCells[i] = new CellData();
        }
    }
}