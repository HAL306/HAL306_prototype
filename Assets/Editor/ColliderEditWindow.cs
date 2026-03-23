using UnityEngine;
using UnityEditor;
using Game.Terrain;

public class TerrainEditorWindow : EditorWindow
{
    private TerrainContext targetContext;
    private Vector2 leftScrollPos;

    private int selectedX = -1;
    private int selectedY = -1;

    // 初期化用
    private int initWidth = 10;
    private int initHeight = 10;
    private float initScale = 1.0f;

    public static TerrainEditorWindow OpenWindow(ref TerrainContext context)
    {
        TerrainEditorWindow window = GetWindow<TerrainEditorWindow>("Terrain Editor");
        window.targetContext = context;
        window.Show();
        return window;
    }

    private void OnGUI()
    {
        // 編集対象のコンポーネントをセット
        targetContext = (TerrainContext)EditorGUILayout.ObjectField("Target Terrain", targetContext, typeof(TerrainContext), true);

        if (targetContext == null)
        {
            EditorGUILayout.HelpBox("TerrainContextがアタッチされたオブジェクトをセットしてください。", MessageType.Info);
            return;
        }

        // --- 初期化されていない場合のUI ---
        if (targetContext.TerrainGrid == null || targetContext.TerrainGrid.RawCells == null || targetContext.TerrainGrid.RawCells.Length == 0)
        {
            EditorGUILayout.LabelField("グリッドの初期化", EditorStyles.boldLabel);
            initWidth = EditorGUILayout.IntField("Width", initWidth);
            initHeight = EditorGUILayout.IntField("Height", initHeight);
            initScale = EditorGUILayout.FloatField("Grid Scale", initScale);

            if (GUILayout.Button("Initialize Grid"))
            {
                Undo.RecordObject(targetContext, "Initialize Terrain Grid");
                targetContext.TerrainGrid = new TerrainGridData(initWidth, initHeight, initScale);
                EditorUtility.SetDirty(targetContext);
            }
            return;
        }

        // --- 左右分割のUI ---
        EditorGUILayout.BeginHorizontal();
        DrawCellList();   // 左：セルリスト
        DrawCellEditor(); // 右：テクスチャ＆編集
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCellList()
    {
        var grid = targetContext.TerrainGrid;

        EditorGUILayout.BeginVertical(GUILayout.Width(200));
        leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos, "box");

        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                bool isSelected = (selectedX == x && selectedY == y);
                GUI.color = isSelected ? Color.cyan : Color.white;

                if (GUILayout.Button($"Cell [{x}, {y}]"))
                {
                    selectedX = x;
                    selectedY = y;
                    GUI.FocusControl(null);
                }
                GUI.color = Color.white;
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawCellEditor()
    {
        var grid = targetContext.TerrainGrid;

        EditorGUILayout.BeginVertical("box");
        DrawTexturePreview(); // テクスチャ描画（グリッド付き）
        GUILayout.Space(20);

        // 選択されたセルが有効範囲内か
        if (grid.InBounds(selectedX, selectedY))
        {
            GUILayout.Label($"Editing Cell [{selectedX}, {selectedY}]", EditorStyles.boldLabel);

            GridCell cell = grid.Get(selectedX, selectedY);

            EditorGUI.BeginChangeCheck();

            cell.solid = EditorGUILayout.Toggle("Solid", cell.solid);
            cell.isStatic = EditorGUILayout.Toggle("Is Static", cell.isStatic);
            // ElementTypeがenumであることを前提としています
            cell.elementType = (ElementType)EditorGUILayout.EnumPopup("Element Type", cell.elementType);
            cell.durability = EditorGUILayout.FloatField("Durability", cell.durability);
            cell.mass = EditorGUILayout.FloatField("Mass", cell.mass);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(targetContext, "Edit Cell Data");
                targetContext.TerrainGrid.Set(selectedX, selectedY, cell);
                EditorUtility.SetDirty(targetContext); // 保存フラグを立てる
            }
        }
        else
        {
            GUILayout.Label("左のリスト、または画像をクリックしてセルを選択してください。");
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawTexturePreview()
    {
        var grid = targetContext.TerrainGrid;
        Texture texture = null;

        // Rendererコンポーネントを取得してからマテリアルにアクセスする
        var renderer = targetContext.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            // カスタムシェーダーで "_MainTexture" というプロパティ名を使っている場合
            texture = renderer.sharedMaterial.GetTexture("_MainTexture");

            // もし上記で取れなかった場合の保険（一般的な標準シェーダーのプロパティ名は "_MainTex" です）
            if (texture == null)
            {
                texture = renderer.sharedMaterial.mainTexture;
            }
        }
        if (texture != null)
        {
            GUILayout.Label("Texture Preview (Click to select cell):", EditorStyles.boldLabel);

            // テクスチャプレビュー用の矩形を確保
            Rect texRect = GUILayoutUtility.GetRect(256, 256, GUILayout.ExpandWidth(false));
            GUI.DrawTexture(texRect, texture, ScaleMode.ScaleToFit);

            // --- 【追加機能】グリッド線とハイライトの描画 ---
            float cellWidth = texRect.width / grid.Width;
            float cellHeight = texRect.height / grid.Height;

            // 選択中のセルを水色でハイライト
            if (grid.InBounds(selectedX, selectedY))
            {
                Rect highlightRect = new Rect(texRect.x + selectedX * cellWidth, texRect.y + selectedY * cellHeight, cellWidth, cellHeight);
                EditorGUI.DrawRect(highlightRect, new Color(0, 1, 1, 0.4f)); // 半透明のシアン
            }

            // グリッド線の描画
            Handles.color = new Color(1, 1, 1, 0.3f); // 半透明の白線
            for (int i = 0; i <= grid.Width; i++) // 縦線
            {
                float x = texRect.x + i * cellWidth;
                Handles.DrawLine(new Vector2(x, texRect.y), new Vector2(x, texRect.yMax));
            }
            for (int i = 0; i <= grid.Height; i++) // 横線
            {
                float y = texRect.y + i * cellHeight;
                Handles.DrawLine(new Vector2(texRect.x, y), new Vector2(texRect.xMax, y));
            }
            Handles.color = Color.white; // 色を戻す
            // ------------------------------------------------

            // テクスチャクリック判定
            Event e = Event.current;
            if (e.type == EventType.MouseDown && texRect.Contains(e.mousePosition))
            {
                Vector2 localPos = e.mousePosition - texRect.position;
                float normX = localPos.x / texRect.width;
                float normY = localPos.y / texRect.height;

                int cx = Mathf.FloorToInt(normX * grid.Width);
                int cy = Mathf.FloorToInt(normY * grid.Height);

                selectedX = Mathf.Clamp(cx, 0, grid.Width - 1);
                selectedY = Mathf.Clamp(cy, 0, grid.Height - 1);

                Repaint();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("対象オブジェクトのRenderer/Materialにテクスチャが設定されていません。", MessageType.Warning);
        }
    }
}