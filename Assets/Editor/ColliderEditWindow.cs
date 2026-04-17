using Game.Terrain;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using static Codice.CM.Common.CmCallContext;

// パレットに使うデータ
struct PaletteCell  
{
    public GridCell cell;
    public bool isPaint;
    public Color color;
}

public class TerrainEditorWindow : EditorWindow
{
    // 編集対象のテレインデータの参照
    private TerrainContext targetContext;

    private TerrainSetting terrainSetting;

    // 実際に編集する仮データ
    // パレットのインデックスを格納する
    [SerializeField] private int[] cellMap = null;

    // シェーダーに渡すデータ
    private Texture texture;            // テクスチャ
    private Vector2 textureScale = new Vector2(0.2f,0.2f);          // テクスチャの表示サイズ
    private Vector2 textureOffset = new Vector2(0.0f, 0.0f);        // テクスチャの位置

    // パレット用のリストと状態管理
    private List<PaletteCell> cellPalette = new List<PaletteCell>();
    private int selectedCellIndex = 0;
    private Vector2 paletteScrollPos;       // パレットリストのスクロール位置
    private Vector2 paintStartPos;          // 矩形選択で編集するときの開始位置
    private Vector2 paintEndPos;            // 矩形選択で編集するときの終了位置7
    private bool isRectPaint = false;               // 矩形選択してるかどうか

    // セル選択ツールを使ってるかどうか
    private bool isSelect = true;

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
    private void OnEnable()
    {
        // UndoやRedoが行われたら、このウィンドウの Repaint() を呼ぶように登録
        Undo.undoRedoPerformed += Repaint;
    }

    private void OnDisable()
    {
        // エラー防止のため、ウィンドウが閉じる時に必ず登録を解除する
        Undo.undoRedoPerformed -= Repaint;
    }

    private void OnGUI()
    {
        if (targetContext == null)
        {
            EditorGUILayout.HelpBox("対象データが失われました。ウィンドウを開き直してください。", MessageType.Warning);
            return;
        }
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
            // セーブの処理は遅らせる
            GUI.FocusControl(null);
            EditorApplication.delayCall += SaveData;
        }
        GUI.backgroundColor = Color.white;
    }

    // 初期化
    private void Init(ref TerrainContext context)
    {
        // 初期化
        targetContext = context;
        gridWidth = targetContext.TerrainGrid.Width;
        gridHeight = targetContext.TerrainGrid.Height;
        cellScale = targetContext.TerrainGrid.GridScale;
        cellMap = new int[gridWidth * gridHeight];
        terrainSetting = targetContext.TerrainSetting;

        // ウィンドウを開いた時に初期パレットを用意する
        InitPaletteAndCellmap();

        // テクスチャが設定されていれば取得する
        var renderer = targetContext.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            // シェーダーからテクスチャを取得する
            texture = renderer.sharedMaterial.GetTexture("_MainTexture") ?? renderer.sharedMaterial.mainTexture;
            textureScale = renderer.sharedMaterial.GetVector("_TextureScale");
            textureOffset = renderer.sharedMaterial.GetVector("_TextureOffset");
        }
    }

    private void DrawInitUI()
    {
        EditorGUILayout.LabelField("グリッドのデータ", EditorStyles.boldLabel);

        // 横並びにする
        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        gridWidth = EditorGUILayout.IntField("Width", gridWidth);
        gridHeight = EditorGUILayout.IntField("Height", gridHeight);
        cellScale = EditorGUILayout.FloatField("Grid Scale", cellScale);

        if (EditorGUI.EndChangeCheck())
        {
            // データの大きさを再設定
            cellMap = new int[gridWidth * gridHeight];
        }
        terrainSetting = (TerrainSetting)EditorGUILayout.ObjectField("TerrainSetting", terrainSetting, typeof(TerrainSetting), false);
        EditorGUILayout.EndHorizontal();
    }

    // --- 左側：カラーパレット（上下スクロールリスト） ---
    private void DrawPaletteList()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(260));

        if (isSelect)   // 選択ツール使用中は色変える
        {
            GUI.backgroundColor = new Color(1.0f, 1.0f, 0.0f);
        }
        if (GUILayout.Button("セル選択ツール", GUILayout.Height(30)))
        {
            // 選択ツール切り替え
            isSelect = !isSelect;
        }
        GUI.backgroundColor = Color.white; // 色を元に戻す

        GUILayout.Label("Cell Palette", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("リストからセルを選択し、右の画像に塗ります。", MessageType.Info);

        // スクロール可能なリスト部分
        paletteScrollPos = EditorGUILayout.BeginScrollView(paletteScrollPos, "box", GUILayout.ExpandHeight(true));

        for (int i = 0; i < cellPalette.Count; i++)
        {
            // 選択中のセルは色を変えてハイライト
            GUI.backgroundColor = (i == selectedCellIndex) ? new Color(0.6f, 0.8f, 1.0f) : Color.white;

            // セルの表示名をパラメータから自動生成
            string brushName = GetBrushName(i, cellPalette[i].cell);

            if (GUILayout.Button(brushName, GUILayout.Height(30)))
            {
                selectedCellIndex = i;
                // 選択ツールオフ
                isSelect = false;
                GUI.FocusControl(null); // 入力フォーカスを外す
            }
        }
        GUI.backgroundColor = Color.white; // 色を元に戻す

        EditorGUILayout.EndScrollView();

        GUILayout.Space(5);

        // 新しいセルの追加ボタン
        if (GUILayout.Button("+ 新しいセルを追加", GUILayout.Height(25)))
        {
            cellPalette.Add(new PaletteCell { cell = { solid = true, durability = 1, mass = 1 }, isPaint = false,color = Color.white});
            selectedCellIndex = cellPalette.Count - 1;  // 追加したものを自動で選択
            paletteScrollPos.y = float.MaxValue;    // 一番下までスクロールさせる
            isSelect = false;                       // 選択ツールオフ
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
            PaletteCell current = cellPalette[selectedCellIndex];

            EditorGUI.BeginChangeCheck();

            current.isPaint = EditorGUILayout.Toggle("IsPaint", current.isPaint);
            current.color = EditorGUILayout.ColorField("Color", current.color);
            current.cell.solid = EditorGUILayout.Toggle("Solid", current.cell.solid);
            current.cell.isStatic = EditorGUILayout.Toggle("Is Static", current.cell.isStatic);
            current.cell.elementType = (ElementType)EditorGUILayout.EnumPopup("Element Type", current.cell.elementType);
            current.cell.durability = EditorGUILayout.FloatField("Durability", current.cell.durability);
            current.cell.mass = EditorGUILayout.FloatField("Mass", current.cell.mass);

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
            float aspect = (float)gridWidth / (float)gridHeight;
            Rect texRect = GUILayoutUtility.GetAspectRect(aspect);
            //Rect texRect = GUILayoutUtility.GetRect(512, 512, GUILayout.ExpandWidth(false));
            

            // --- ペイント（クリック＆ドラッグ）の処理 ---
            Event e = Event.current;
            if (texRect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                {
                    if (e.button == 0 && cellPalette.Count > 0) // 左クリックかつセルが存在する場合
                    {
                        Vector2 localPos = e.mousePosition - texRect.position;
                        int cx = Mathf.Clamp(Mathf.FloorToInt((localPos.x / texRect.width) * gridWidth), 0, gridWidth - 1);
                        int cy = Mathf.Clamp(Mathf.FloorToInt((localPos.y / texRect.height) * gridHeight), 0, gridHeight - 1);

                        // 上下逆にする
                        cy = gridHeight - (cy + 1);

                        if (isSelect)    // 選択ツール使用中は
                        {
                            // セルと同じセルを選択する
                            selectedCellIndex = cellMap[cy * gridWidth + cx];
                        }
                        else
                        {
                            // 選択中のブラシのデータをセルに適用
                            cellMap[cy * gridWidth + cx] = selectedCellIndex;
                        }

                        e.Use();
                    }
                    else if (e.button == 1 && cellPalette.Count > 0 && !isSelect) // 右クリックかつセルが存在するかつ選択ツール使用してない　場合
                    {
                        if(e.type == EventType.MouseDown)   // 押した瞬間に初期化
                        {
                            // はみ出さないようにClampして開始位置を記録
                            paintStartPos = new Vector2(
                                Mathf.Clamp(e.mousePosition.x, texRect.xMin, texRect.xMax),
                                Mathf.Clamp(e.mousePosition.y, texRect.yMin, texRect.yMax)
                            );
                            paintEndPos = paintStartPos;
                            isRectPaint = true;
                            e.Use();
                        }
                    }
                }
            }
            // 右ドラッグ中
            if (e.type == EventType.MouseDrag && isRectPaint)
            {
                // はみ出さないようにClampして終了位置を更新
                paintEndPos = new Vector2(
                    Mathf.Clamp(e.mousePosition.x, texRect.xMin, texRect.xMax),
                    Mathf.Clamp(e.mousePosition.y, texRect.yMin, texRect.yMax)
                );
                e.Use();
            }

            // 右クリックを離した時
            if (e.type == EventType.MouseUp && e.button == 1 && isRectPaint)
            {
                isRectPaint = false;

                // texRect基準のローカル座標に変換
                Vector2 localStartPos = paintStartPos - texRect.position;
                Vector2 localEndPos = paintEndPos - texRect.position;

                // マウス座標(ピクセル)からグリッド座標(インデックス)への変換
                int startCX = Mathf.Clamp(Mathf.FloorToInt((localStartPos.x / texRect.width) * gridWidth), 0, gridWidth - 1);
                int endCX = Mathf.Clamp(Mathf.FloorToInt((localEndPos.x / texRect.width) * gridWidth), 0, gridWidth - 1);
                int startCY = Mathf.Clamp(Mathf.FloorToInt((localStartPos.y / texRect.height) * gridHeight), 0, gridHeight - 1);
                int endCY = Mathf.Clamp(Mathf.FloorToInt((localEndPos.y / texRect.height) * gridHeight), 0, gridHeight - 1);

                // UnityGUIの座標系(左上原点)から、グリッドの座標系(左下原点)に上下反転
                startCY = gridHeight - (startCY + 1);
                endCY = gridHeight - (endCY + 1);

                // forループを回すための Min/Max を計算
                int minCX = Mathf.Min(startCX, endCX);
                int maxCX = Mathf.Max(startCX, endCX);
                int minCY = Mathf.Min(startCY, endCY);
                int maxCY = Mathf.Max(startCY, endCY);

                // Undoできるようにする
                Undo.RecordObject(this, "Square Paint");

                for (int y = minCY; y <= maxCY; y++)
                {
                    for(int x = minCX; x <= maxCX; x++)
                    {
                        cellMap[y * gridWidth + x] = selectedCellIndex;
                    }
                }
                EditorUtility.SetDirty(this);

                GUI.changed = true;
                e.Use();
            }

            // 表示
            GUI.DrawTexture(texRect, texture, ScaleMode.ScaleToFit);

            // 
            float cellWidth = texRect.width / gridWidth;
            float cellHeight = texRect.height / gridHeight;

            // 塗られているセルの可視化
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    // 可視化する領域の設定
                    // グリッドデータは上下逆にする
                    Rect fillRect = new Rect(texRect.x + x * cellWidth, texRect.y + texRect.height - (y + 1) * cellHeight, cellWidth, cellHeight);
                    PaletteCell paletteCell = cellPalette[cellMap[y * gridWidth + x]];
                    if (!paletteCell.cell.solid)
                    {
                        // 当たり判定がない部分を灰色にする
                        EditorGUI.DrawRect(fillRect, new Color(0.3f, 0.3f, 0.3f, 0.4f));
                    }
                    else
                    {
                        if (paletteCell.isPaint)
                        {
                            // セルに応じた色付ける
                            EditorGUI.DrawRect(fillRect, new Color(paletteCell.color.r, paletteCell.color.g, paletteCell.color.b, 0.4f));
                        }
                    }
                }
            }

            // グリッド線の描画
            Handles.color = new Color(1, 1, 1, 0.3f);
            for (int i = 0; i <= gridWidth; i++)
            {
                // 縦線表示
                Handles.DrawLine(new Vector2(texRect.x + i * cellWidth, texRect.y), new Vector2(texRect.x + i * cellWidth, texRect.yMax));
            }
            for (int i = 0; i <= gridHeight; i++)
            {
                // 横線表示
                Handles.DrawLine(new Vector2(texRect.x, texRect.y + i * cellHeight), new Vector2(texRect.xMax, texRect.y + i * cellHeight));
            }
            Handles.color = Color.white;

            // 矩形ペイントの範囲プレビュー描画
            if (isRectPaint)
            {
                float minX = Mathf.Min(paintStartPos.x, paintEndPos.x);
                float maxX = Mathf.Max(paintStartPos.x, paintEndPos.x);
                float minY = Mathf.Min(paintStartPos.y, paintEndPos.y);
                float maxY = Mathf.Max(paintStartPos.y, paintEndPos.y);

                Rect paintRect = new Rect(minX, minY, maxX - minX, maxY - minY);
                EditorGUI.DrawRect(paintRect, new Color(0.3f, 0.3f, 0.3f, 0.5f)); // 選択中とわかりやすいように半透明に
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

        Undo.RecordObject(targetContext, "Save Terrain Data");
        // グリッドデータを作る
        TerrainGridData grid = new TerrainGridData(gridWidth,gridHeight,cellScale);

        for(int i = 0; i < cellMap.Length; i++)
        {
            grid.Set(i, cellPalette[cellMap[i]].cell);   // インデックスからグリッドデータに変える
        }

        targetContext.TerrainGrid = grid;

        if(terrainSetting == null)
        {
            Debug.LogWarning("terrainSettingが選択されていません。");
            return;
        }
        targetContext.TerrainSetting = terrainSetting;

        if (PrefabUtility.IsPartOfPrefabInstance(targetContext))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(targetContext);
        }
        EditorUtility.SetDirty(targetContext);  // unityに変更を通知する
        if (!EditorUtility.IsPersistent(targetContext)) // アセットではなくシーン上に存在する場合
        {
            EditorSceneManager.MarkSceneDirty(targetContext.gameObject.scene);  // シーンに*マークを付ける
        }
        AssetDatabase.SaveAssets();     // アセットの変更をディスクに書き込む
        Debug.Log($"[{targetContext.gameObject.name}] の TerrainGrid データを保存しました。");

        // 現在選択されているオブジェクトを取得
        GameObject targetObject = targetContext.gameObject;

        if (targetObject == null)   // nullチェック
        {
            Debug.LogWarning("オブジェクトが選択されていません。");
            return;
        }

        // 当たり判定を生成
        TerrainPolygon terrainPolygon = targetObject.GetComponent<TerrainPolygon>();

        if (terrainPolygon == null)
        {
            Debug.LogWarning($"{targetObject.name} には TerrainPolygon コンポーネントがありません。");
        } 
        else if (terrainPolygon.InitPolygon()) 
        {
            Debug.Log($"[{targetContext.gameObject.name}] の当たり判定を生成しました。");
        }
        else
        {
            Debug.Log($"[{targetContext.gameObject.name}] の当たり判定の生成に失敗しました。");
        }

        // シェーダーの変更を保存
        // renderer取得
        Renderer renderer = targetObject.GetComponent<Renderer>();
        if (renderer == null)       // nullチェック
        {
            Debug.LogWarning($"{targetObject.name} には Renderer コンポーネントがありません。");
            return;
        }

        // 古いマテリアルを消す
        Material material = renderer.sharedMaterial;    // マテリアル取得
        string path = null;
        if (material != null)
        {
            path = AssetDatabase.GetAssetPath(material);    // マテリアルのパスを取得
        }

        // 「Assets/」から始まる、自分のプロジェクト内のファイルのみ削除を許可
        if (!string.IsNullOrEmpty(path) && path.StartsWith("Assets/Materials/Breakable"))
        {
            if (AssetDatabase.DeleteAsset(path))
            {
                Debug.Log($"{targetObject.name} にアタッチされていたマテリアルを削除しました。");
            }
            else
            {
                Debug.Log($"{targetObject.name} にアタッチされていたマテリアルを削除出来ませんでした。");
            }
        }
        else
        {
            Debug.LogWarning("組み込みマテリアル等のため、削除をスキップしました。");
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
        newMaterial.SetVector("_TextureScale", textureScale);
        newMaterial.SetVector("_TextureOffset", textureOffset);

        // プロジェクト内にアセット（.matファイル）として保存する
        // GenerateUniqueAssetPathを使うことで、同名ファイルがある場合は "NewMaterial 1.mat" のように自動で連番をつける
        string assetPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Materials/Breakable/NewMaterial.mat");
        AssetDatabase.CreateAsset(newMaterial, assetPath);
        AssetDatabase.SaveAssets(); // アセットの変更をディスクに書き込む

        // オブジェクトにアタッチする
        Undo.RecordObject(renderer, "Assign New Material");
        renderer.sharedMaterial = newMaterial;

        // 変更をマークして、Projectウィンドウ上で選択状態にする
        EditorUtility.SetDirty(renderer);
        AssetDatabase.SaveAssets();     // アセットの変更をディスクに書き込む
        //EditorGUIUtility.PingObject(newMaterial);

        // 裏側での処理が終わったことをUnityに知らせ、画面を強制的に更新させる
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.Repaint(); // シーンビューの更新
        }
        Repaint(); // このエディタウィンドウ自身の更新
    }

    // パレットの初期化
    /// <summary>
    // オブジェクトに記録されているTerrainContextに含まれるセルの情報を反映させる
    /// </summary>
    private void InitPaletteAndCellmap()
    {
        if (cellPalette.Count != 0)
            return;

        // どんなデータでも必ずパレットに登録するデータ
        cellPalette.Add(new PaletteCell { cell = { solid = false, isStatic = false, durability = 0, mass = 0 },isPaint = false, color = Color.white});      // 0: 消しゴム
        cellPalette.Add(new PaletteCell { cell = { solid = true, isStatic = false, durability = 1, mass = 1 }, isPaint = false, color = Color.white });     // 1: 固定壁

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
                for (int i = 0; i < cellPalette.Count; i++)
                {
                    // 存在したら
                    if(cell.Equals(cellPalette[i].cell))    
                    {
                        // パレットに追加しない
                        IsAdd = false;
                        // 仮データに登録
                        cellMap[y * grid.Width + x] = i;
                        break;
                    }
                }

                if (IsAdd)
                {
                    // パレットに登録
                    cellPalette.Add(new PaletteCell { cell = cell, isPaint = false, color = Color.white });
                    // 仮データに登録
                    cellMap[y * grid.Width + x] = cellPalette.Count - 1;
                }
            }
        }
    }

    // --- 右側：テクスチャやシェーダーに渡すデータを表示、入力する
    private void DrawTextureData()
    {
        EditorGUILayout.BeginVertical("box",GUILayout.Width(260));
        GUILayout.Label("テクスチャ設定", EditorStyles.boldLabel);
        texture = (Texture)EditorGUILayout.ObjectField("Texture", texture, typeof(Texture), false, GUILayout.ExpandWidth(false));
        textureScale = EditorGUILayout.Vector2Field("TextureScale",textureScale, GUILayout.ExpandWidth(false));
        textureOffset = EditorGUILayout.Vector2Field("TextureOffset", textureOffset,GUILayout.ExpandWidth(false));
        EditorGUILayout.EndVertical();
    }
}