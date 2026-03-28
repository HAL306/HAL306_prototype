using Game.Terrain;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;

public class TerrainEditorWindow : EditorWindow
{
    private TerrainContext targetContext;

    // �p���b�g�p�̃��X�g�Ə�ԊǗ�
    private List<GridCell> cellPalette = new List<GridCell>();
    private int selectedCellIndex = 0;
    private Vector2 paletteScrollPos; // �p���b�g���X�g�̃X�N���[���ʒu

    // �������p
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
        // �O���b�h�̑傫���A�Z���̑傫���̂���͂���UI
        DrawInitUI();

        EditorGUILayout.BeginHorizontal();
        DrawPaletteList();    // ���F�㉺�X�N���[���\�ȃu���V���X�g
        DrawTextureCanvas();  // �E�F�L�����o�X�i�h��G���A�j
        EditorGUILayout.EndHorizontal();

        // �ۑ��{�^��
        GUILayout.Space(10);
        GUI.backgroundColor = new Color(0.6f, 1.0f, 0.6f);
        if (GUILayout.Button("Save Terrain Data", GUILayout.Height(35)))
        {
            SaveData();
        }
        GUI.backgroundColor = Color.white;
    }

    // ������
    private void Init(ref TerrainContext context)
    {
        targetContext = context; 

        // �E�B���h�E���J�������ɏ����p���b�g��p�ӂ���
        InitPalette();

        // ������
        gridWidth = targetContext.TerrainGrid.Width;
        gridHeight = targetContext.TerrainGrid.Height;
        cellScale = targetContext.TerrainGrid.GridScale;
    }

    private void DrawInitUI()
    {
        EditorGUILayout.LabelField("�O���b�h�̃f�[�^", EditorStyles.boldLabel);

        // �����тɂ���
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

    // --- �����F�J���[�p���b�g�i�㉺�X�N���[�����X�g�j ---
    private void DrawPaletteList()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(260));
        GUILayout.Label("Cell Palette", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("���X�g����Z����I�����A�E�̉摜�ɓh��܂��B", MessageType.Info);

        // �X�N���[���\�ȃ��X�g����
        paletteScrollPos = EditorGUILayout.BeginScrollView(paletteScrollPos, "box", GUILayout.ExpandHeight(true));

        for (int i = 0; i < cellPalette.Count; i++)
        {
            // �I�𒆂̃Z���͐F��ς��ăn�C���C�g
            GUI.backgroundColor = (i == selectedCellIndex) ? new Color(0.6f, 0.8f, 1.0f) : Color.white;

            // �Z���̕\�������p�����[�^���玩������
            string brushName = GetBrushName(i, cellPalette[i]);

            if (GUILayout.Button(brushName, GUILayout.Height(30)))
            {
                selectedCellIndex = i;
                GUI.FocusControl(null); // ���̓t�H�[�J�X���O��
            }
        }
        GUI.backgroundColor = Color.white; // �F�����ɖ߂�

        EditorGUILayout.EndScrollView();
        
        GUILayout.Space(5);

        // �V�����Z���̒ǉ��{�^��
        if (GUILayout.Button("+ �V�����Z����ǉ�", GUILayout.Height(25)))
        {
            cellPalette.Add(new GridCell { solid = true, durability = 1, mass = 1 });
            selectedCellIndex = cellPalette.Count - 1; // �ǉ��������̂������őI��
            paletteScrollPos.y = float.MaxValue; // ��ԉ��܂ŃX�N���[��������
        }

        GUILayout.Space(10);

        // --- �I�𒆂̃Z���̃p�����[�^�ҏW ---
        DrawBrushSettings();

        EditorGUILayout.EndVertical();
    }

    // �Z���̕\�����𐶐�����w���p�[�֐�
    private string GetBrushName(int index, GridCell cell)
    {
        if (!cell.solid) return $"{index}: �����S�� (Empty)";
        return $"{index}: �u���b�N ({cell.elementType})";
    }

    // �I�𒆂̃Z���̃v���p�e�B��`��
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

            // �l�ɕύX���������ꍇ�̓��X�g���̃f�[�^���X�V
            if (EditorGUI.EndChangeCheck())
            {
                cellPalette[selectedCellIndex] = current;
            }
        }
    }

    // --- �E���F�L�����o�X�i�e�N�X�`���ƃy�C���g�����j ---
    private void DrawTextureCanvas()
    {
        var grid = targetContext.TerrainGrid;
        Texture texture = null;

        var renderer = targetContext.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            // �V�F�[�_�[����e�N�X�`�����擾����
            texture = renderer.sharedMaterial.GetTexture("_MainTexture") ?? renderer.sharedMaterial.mainTexture;
        }

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Canvas (Click & Drag to Paint)", EditorStyles.boldLabel);

        // �e�N�X�`����\��
        if (texture != null)
        {
            // �\������g�̃T�C�Y�ݒ�
            Rect texRect = GUILayoutUtility.GetRect(512, 512, GUILayout.ExpandWidth(false));
            // �\��
            GUI.DrawTexture(texRect, texture, ScaleMode.ScaleToFit);

            // 
            float cellWidth = texRect.width / grid.Width;
            float cellHeight = texRect.height / grid.Height;

            // �h���Ă���Z���̉���
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    // ��������̈�̐ݒ�
                    // �O���b�h�f�[�^�͏㉺�t�ɂ���
                    Rect fillRect = new Rect(texRect.x + x * cellWidth, texRect.y + texRect.height - (y + 1) * cellHeight, cellWidth, cellHeight);
                    if (!grid.Get(x, y).solid)
                    {
                        // �h���ĂȂ��������D�F�ɂ���
                        EditorGUI.DrawRect(fillRect, new Color(0.3f, 0.3f, 0.3f, 0.8f));
                    }
                }
            }

            // �O���b�h���̕`��
            Handles.color = new Color(1, 1, 1, 0.3f);
            for (int i = 0; i <= grid.Width; i++)
            {
                // �c���\��
                Handles.DrawLine(new Vector2(texRect.x + i * cellWidth, texRect.y), new Vector2(texRect.x + i * cellWidth, texRect.yMax));
            }
            for (int i = 0; i <= grid.Height; i++)
            {
                // �����\��
                Handles.DrawLine(new Vector2(texRect.x, texRect.y + i * cellHeight), new Vector2(texRect.xMax, texRect.y + i * cellHeight));
            }
            Handles.color = Color.white;

            // --- �y�C���g�i�N���b�N���h���b�O�j�̏��� ---
            Event e = Event.current;
            if (texRect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                {
                    if (e.button == 0 && cellPalette.Count > 0) // ���N���b�N���Z�������݂���ꍇ
                    {
                        Vector2 localPos = e.mousePosition - texRect.position;
                        int cx = Mathf.Clamp(Mathf.FloorToInt((localPos.x / texRect.width) * grid.Width), 0, grid.Width - 1);
                        int cy = Mathf.Clamp(Mathf.FloorToInt((localPos.y / texRect.height) * grid.Height), 0, grid.Height - 1);

                        // �㉺�t�ɂ���
                        cy = grid.Height - (cy + 1);

                        // �I�𒆂̃u���V�̃f�[�^���Z���ɓK�p
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
            EditorGUILayout.HelpBox("�ΏۃI�u�W�F�N�g��Renderer/Material�Ƀe�N�X�`�����ݒ肳��Ă��܂���B", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    private void SaveData()
    {
        if (targetContext == null) return;
        EditorUtility.SetDirty(targetContext);
        if (!EditorUtility.IsPersistent(targetContext))
        {
            EditorSceneManager.MarkSceneDirty(targetContext.gameObject.scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[{targetContext.gameObject.name}] �� TerrainGrid �f�[�^��ۑ����܂����B");
    }

    // �p���b�g�̏�����
    /// <summary>
    // �I�u�W�F�N�g�ɋL�^����Ă���TerrainContext�Ɋ܂܂��Z���̏��𔽉f������
    /// </summary>
    private void InitPalette()
    {
        if (cellPalette.Count != 0)
            return;

        // �ǂ�ȃf�[�^�ł��K���p���b�g�ɓo�^����f�[�^
        cellPalette.Add(new GridCell { solid = false, isStatic = false, durability = 0, mass = 0 });   // 0: �����S��
        cellPalette.Add(new GridCell { solid = true, isStatic = false, durability = 1, mass = 1 });    // 1: �Œ��

        // �O���b�h�f�[�^���o��
        TerrainGridData grid = targetContext.TerrainGrid;

        // �p���b�g�ɓo�^����
        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                // �p���b�g�ɓo�^���邩�ǂ���
                bool IsAdd = true;
                GridCell cell = grid.Get(x, y);

                // �p���b�g�Ɋ��ɑ��݂��邩�ǂ����m�F����
                for(int i = 0; i < cellPalette.Count; i++)
                {
                    // ���݂�����
                    if(cell.Equals(cellPalette[i]))
                    {
                        // �p���b�g�ɒǉ����Ȃ�
                        IsAdd = false;
                        break;
                    }
                }

                if (IsAdd)
                {
                    // �p���b�g�ɓo�^
                    cellPalette.Add((GridCell)cell);
                }
            }
        }
    }
}