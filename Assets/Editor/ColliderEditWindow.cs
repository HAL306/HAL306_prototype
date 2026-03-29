using Game.Terrain;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Codice.CM.Common.CmCallContext;

public class TerrainEditorWindow : EditorWindow
{
    // 編集対象のテレインデータ
    private TerrainContext targetContext;

    // シェーダーに渡すデータ
    Texture texture;            // テクスチャ
    Vector2 textureScale = new Vector2(0.2f,0.2f);          // テクスチャの表示サイズ
    Vector2 textureOffset = new Vector2(0.0f, 0.0f);        // テクスチャの位置

    // パレット用のリストと状態管理
    private List<GridCell> cellPalette = new List<GridCell>();
    private int selectedCellIndex = 0;
    private Vector2 paletteScrollPos; // パレットリストのスクロール位置

    // 初期化用
    private int gridWidth = 10;
    private int gridHeight = 10;
    private float cellScale = 0.2f;

    public static TerrainEditorWindow OpenWindow(ref TerrainContext context)
    {
        TerrainEditorWindow window = GetWindow<TerrainEditorWindow>("Terrain Palette Editor");
        window.Init(ref context);
        window.Show();
        return window;
    }

    private void OnGUI()
    {
        // グリッドの大きさ、セルの大きさのを入力するUI
        DrawInitUI();

        EditorGUILayout.BeginHorizontal();
        DrawPaletteList();      // 左：上下スクロール可能なブラシリスト
        DrawTextureCanvas();    // 中央：キャンバス（塗るエリア）
        DrawTextureData();      // 右:シェーダーに渡すデータ
        EditorGUILayout.EndHorizontal();

        // 保存ボタン
        GUILayout.Space(10);
        GUI.backgroundColor = new Color(0.6f, 1.0f, 0.6f);
        if (GUILayout.Button("Save Terrain Data", GUILayout.Height(35)))
        {
            SaveData();
        }
        GUI.backgroundColor = Color.white;
    }

    // 初期化
    private void Init(ref TerrainContext context)
    {
        targetContext = context; 

        // ウィンドウを開いた時に初期パレットを用意する
        InitPalette();

        // 初期化
        gridWidth = targetContext.TerrainGrid.Width;
        gridHeight = targetContext.TerrainGrid.Height;
        cellScale = targetContext.TerrainGrid.GridScale;

        // テクスチャが設定されていれば取得する
        var renderer = targetContext.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            // シェーダーからテクスチャを取得する
            texture = renderer.sharedMaterial.GetTexture("_MainTexture") ?? renderer.sharedMaterial.mainTexture;
        }
    }

    private void DrawInitUI()
    {
        EditorGUILayout.LabelField("グリッドのデータ", EditorStyles.boldLabel);

        // 横並びにする
        EditorGUILayout.BeginHorizontal();

        gridWidth = EditorGUILayout.IntField("Width", gridWidth);
        gridHeight = EditorGUILayout.IntField("Height", gridHeight);
        cellScale = EditorGUILayout.FloatField("Grid Scale", cellScale);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Initialize Grid"))
        {
            Undo.RecordObject(targetContext, "Initialize Terrain Grid");
            targetContext.TerrainGrid = new TerrainGridData(gridWidth, gridHeight, cellScale);
            EditorUtility.SetDirty(targetContext);
        }
    }

    // --- 左側：カラーパレット（上下スクロールリスト） ---
    private void DrawPaletteList()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(260));
        GUILayout.Label("Cell Palette", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("リストからセルを選択し、右の画像に塗ります。", MessageType.Info);

        // スクロール可能なリスト部分
        paletteScrollPos = EditorGUILayout.BeginScrollView(paletteScrollPos, "box", GUILayout.ExpandHeight(true));

        for (int i = 0; i < cellPalette.Count; i++)
        {
            // 選択中のセルは色を変えてハイライト
            GUI.backgroundColor = (i == selectedCellIndex) ? new Color(0.6f, 0.8f, 1.0f) : Color.white;

            // セルの表示名をパラメータから自動生成
            string brushName = GetBrushName(i, cellPalette[i]);

            if (GUILayout.Button(brushName, GUILayout.Height(30)))
            {
                selectedCellIndex = i;
                GUI.FocusControl(null); // 入力フォーカスを外す
            }
        }
        GUI.backgroundColor = Color.white; // 色を元に戻す

        EditorGUILayout.EndScrollView();
        
        GUILayout.Space(5);

        // 新しいセルの追加ボタン
        if (GUILayout.Button("+ 新しいセルを追加", GUILayout.Height(25)))
        {
            cellPalette.Add(new GridCell { solid = true, durability = 1, mass = 1 });
            selectedCellIndex = cellPalette.Count - 1; // 追加したものを自動で選択
            paletteScrollPos.y = float.MaxValue; // 一番下までスクロールさせる
        }

        GUILayout.Space(10);

        // --- 選択中のセルのパラメータ編集 ---
        DrawBrushSettings();

        EditorGUILayout.EndVertical();
    }

    // セルの表示名を生成するヘルパー関数
    private string GetBrushName(int index, GridCell cell)
    {
        if (!cell.solid) return $"{index}: 消しゴム (Empty)";
        return $"{index}: ブロック ({cell.elementType})";
    }

    // 選択中のセルのプロパティを描画
    private void DrawBrushSettings()
    {
        GUILayout.Label("Selected Brush Settings", EditorStyles.boldLabel);

        if (selectedCellIndex >= 0 && selectedCellIndex < cellPalette.Count)
        {
            GridCell current = cellPalette[selectedCellIndex];

            EditorGUI.BeginChangeCheck();
            current.solid = EditorGUILayout.Toggle("Solid", current.solid);
            current.isStatic = EditorGUILayout.Toggle("Is Static", current.isStatic);
            current.elementType = (ElementType)EditorGUILayout.EnumPopup("Element Type", current.elementType);
            current.durability = EditorGUILayout.FloatField("Durability", current.durability);
            current.mass = EditorGUILayout.FloatField("Mass", current.mass);

            // 値に変更があった場合はリスト内のデータを更新
            if (EditorGUI.EndChangeCheck())
            {
                cellPalette[selectedCellIndex] = current;
            }
        }
    }

    // --- 中央：キャンバス（テクスチャとペイント処理） ---
    private void DrawTextureCanvas()
    {
        var grid = targetContext.TerrainGrid;

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Canvas (Click & Drag to Paint)", EditorStyles.boldLabel);

        // テクスチャを表示
        if (texture != null)
        {
            // 表示する枠のサイズ設定
            Rect texRect = GUILayoutUtility.GetRect(512, 512, GUILayout.ExpandWidth(false));
            // 表示
            GUI.DrawTexture(texRect, texture, ScaleMode.ScaleToFit);

            // 
            float cellWidth = texRect.width / grid.Width;
            float cellHeight = texRect.height / grid.Height;

            // 塗られているセルの可視化
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    // 可視化する領域の設定
                    // グリッドデータは上下逆にする
                    Rect fillRect = new Rect(texRect.x + x * cellWidth, texRect.y + texRect.height - (y + 1) * cellHeight, cellWidth, cellHeight);
                    if (!grid.Get(x, y).solid)
                    {
                        // 塗られてない部分を灰色にする
                        EditorGUI.DrawRect(fillRect, new Color(0.3f, 0.3f, 0.3f, 0.4f));
                    }
                }
            }

            // グリッド線の描画
            Handles.color = new Color(1, 1, 1, 0.3f);
            for (int i = 0; i <= grid.Width; i++)
            {
                // 縦線表示
                Handles.DrawLine(new Vector2(texRect.x + i * cellWidth, texRect.y), new Vector2(texRect.x + i * cellWidth, texRect.yMax));
            }
            for (int i = 0; i <= grid.Height; i++)
            {
                // 横線表示
                Handles.DrawLine(new Vector2(texRect.x, texRect.y + i * cellHeight), new Vector2(texRect.xMax, texRect.y + i * cellHeight));
            }
            Handles.color = Color.white;

            // --- ペイント（クリック＆ドラッグ）の処理 ---
            Event e = Event.current;
            if (texRect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                {
                    if (e.button == 0 && cellPalette.Count > 0) // 左クリックかつセルが存在する場合
                    {
                        Vector2 localPos = e.mousePosition - texRect.position;
                        int cx = Mathf.Clamp(Mathf.FloorToInt((localPos.x / texRect.width) * grid.Width), 0, grid.Width - 1);
                        int cy = Mathf.Clamp(Mathf.FloorToInt((localPos.y / texRect.height) * grid.Height), 0, grid.Height - 1);

                        // 上下逆にする
                        cy = grid.Height - (cy + 1);

                        // 選択中のブラシのデータをセルに適用
                        Undo.RecordObject(targetContext, "Paint Terrain Cell");
                        targetContext.TerrainGrid.Set(cx, cy, cellPalette[selectedCellIndex]);
                        EditorUtility.SetDirty(targetContext);

                        e.Use();
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("対象オブジェクトのRenderer/Materialにテクスチャが設定されていません。", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    private void SaveData()
    {
        if (targetContext == null) return;      // NULLチェック
        EditorUtility.SetDirty(targetContext);  // unityに変更を通知する
        if (!EditorUtility.IsPersistent(targetContext)) // アセットではなくシーン上に存在する場合
        {
            EditorSceneManager.MarkSceneDirty(targetContext.gameObject.scene);  // シーンに*マークを付ける
        }
        AssetDatabase.SaveAssets();     // アセットの変更をディスクに書き込む
        Debug.Log($"[{targetContext.gameObject.name}] の TerrainGrid データを保存しました。");

        // シェーダーの変更を保存
        // 現在選択されているオブジェクトを取得
        GameObject targetObject = targetContext.gameObject;

        if (targetObject == null)   // nullチェック
        {
            Debug.LogWarning("オブジェクトが選択されていません。");
            return;
        }

        // renderer取得
        Renderer renderer = targetObject.GetComponent<Renderer>();
        if (renderer == null)       // nullチェック
        {
            Debug.LogWarning($"{targetObject.name} には Renderer コンポーネントがありません。");
            return;
        }

        // 新しいマテリアルをメモリ上に生成
        Shader shader = Shader.Find("Shader Graphs/SH_Terrain");
        if (shader == null)
        {
            Debug.LogError("指定したシェーダーが見つかりません。");
            return;
        }

        Material newMaterial = new Material(shader);

        // データを反映させる
        newMaterial.SetTexture("_MainTexture",texture);
        newMaterial.SetTextureScale("_TextureScale", textureScale);
        newMaterial.SetTextureOffset("_TextureOffset", textureOffset);

        // プロジェクト内にアセット（.matファイル）として保存する
        // GenerateUniqueAssetPathを使うことで、同名ファイルがある場合は "NewMaterial 1.mat" のように自動で連番をつける
        string assetPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Materials/NewMaterial.mat");
        AssetDatabase.CreateAsset(newMaterial, assetPath);
        AssetDatabase.SaveAssets(); // アセットの変更をディスクに書き込む

        // オブジェクトにアタッチする
        Undo.RecordObject(renderer, "Assign New Material");
        renderer.sharedMaterial = newMaterial;

        // 変更をマークして、Projectウィンドウ上で選択状態にする
        EditorUtility.SetDirty(renderer);
        EditorGUIUtility.PingObject(newMaterial);
    }

    // パレットの初期化
    /// <summary>
    // オブジェクトに記録されているTerrainContextに含まれるセルの情報を反映させる
    /// </summary>
    private void InitPalette()
    {
        if (cellPalette.Count != 0)
            return;

        // どんなデータでも必ずパレットに登録するデータ
        cellPalette.Add(new GridCell { solid = false, isStatic = false, durability = 0, mass = 0 });   // 0: 消しゴム
        cellPalette.Add(new GridCell { solid = true, isStatic = false, durability = 1, mass = 1 });    // 1: 固定壁

        // グリッドデータ取り出す
        TerrainGridData grid = targetContext.TerrainGrid;

        // パレットに登録する
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                // パレットに登録するかどうか
                bool IsAdd = true;
                GridCell cell = grid.Get(x, y);

                // パレットに既に存在するかどうか確認する
                for(int i = 0; i < cellPalette.Count; i++)
                {
                    // 存在したら
                    if(cell.Equals(cellPalette[i]))
                    {
                        // パレットに追加しない
                        IsAdd = false;
                        break;
                    }
                }

                if (IsAdd)
                {
                    // パレットに登録
                    cellPalette.Add((GridCell)cell);
                }
            }
        }
    }

    // テクスチャやシェーダーに渡すデータを表示、入力する
    private void DrawTextureData()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("テクスチャ設定", EditorStyles.boldLabel);
        texture = (Texture)EditorGUILayout.ObjectField("Texture", texture, typeof(Texture), false);
        textureScale = EditorGUILayout.Vector2Field("TextureScale",textureScale);
        textureOffset = EditorGUILayout.Vector2Field("TextureOffset", textureOffset);
        EditorGUILayout.EndVertical();
    }
}