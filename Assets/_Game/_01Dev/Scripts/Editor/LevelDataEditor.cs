#if UNITY_EDITOR
using System.Collections.Generic;
using BusJam.Data;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    private static readonly Dictionary<PassengerColor, Color> ColorOf = new()
    {
        { PassengerColor.Red, Color.red },
        { PassengerColor.Blue, Color.blue },
        { PassengerColor.Green, Color.green },
        { PassengerColor.Yellow, Color.yellow },
        { PassengerColor.Purple, Color.magenta },
        { PassengerColor.Orange, new Color(1f, 0.5f, 0f) }
    };

    private static readonly Color VoidGrey = new(0.25f, 0.25f, 0.25f);

    private readonly Cell _brush = new() { type = CellType.Empty, colour = PassengerColor.Red };
    private ReorderableList _busList;

    private LevelData _level;
    private SerializedProperty _rowsProp, _colsProp, _cellsProp, _busesProp, _timeLimitProp, _waitingAreaSizeProp;

    private Vector2 _scrollPosition;
    private SerializedObject _so;

    private void OnEnable()
    {
        _level = (LevelData)target;
        CacheProps();
    }

    public override void OnInspectorGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        DrawHeader();
        GUILayout.Space(3);
        DrawGrid();
        GUILayout.Space(6);
        DrawPalette();
        GUILayout.Space(10);
        DrawBusQueue();

        EditorGUILayout.EndScrollView();
    }

    private new void DrawHeader()
    {
        _so.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_timeLimitProp, new GUIContent("Time Limit"));
        EditorGUILayout.PropertyField(_waitingAreaSizeProp, new GUIContent("Waiting Area Size"));
        EditorGUILayout.PropertyField(_rowsProp, new GUIContent("Rows"));
        EditorGUILayout.PropertyField(_colsProp, new GUIContent("Columns"));

        if (EditorGUI.EndChangeCheck())
        {
            var need = Mathf.Max(1, _rowsProp.intValue) * Mathf.Max(1, _colsProp.intValue);
            if (_cellsProp.arraySize != need)
            {
                _cellsProp.arraySize = need;
                for (var i = 0; i < need; i++)
                {
                    var cellProp = _cellsProp.GetArrayElementAtIndex(i);
                    if (cellProp != null)
                    {
                        var typeProp = cellProp.FindPropertyRelative(nameof(Cell.type));
                        var colourProp = cellProp.FindPropertyRelative(nameof(Cell.colour));
                        if (typeProp != null) typeProp.enumValueIndex = (int)CellType.Empty;
                        if (colourProp != null) colourProp.enumValueIndex = (int)PassengerColor.Red;
                    }
                }
            }

            _so.ApplyModifiedProperties();
            return;
        }

        _so.ApplyModifiedProperties();
    }

    private void CacheProps()
    {
        if (_level == null)
        {
            _so = null;
            return;
        }

        _so = new SerializedObject(_level);

        _rowsProp = _so.FindProperty(nameof(LevelData.rows));
        _colsProp = _so.FindProperty(nameof(LevelData.cols));
        _cellsProp = _so.FindProperty(nameof(LevelData.cells));
        _busesProp = _so.FindProperty(nameof(LevelData.buses));
        _timeLimitProp = _so.FindProperty(nameof(LevelData.timeLimit));
        _waitingAreaSizeProp = _so.FindProperty(nameof(LevelData.waitingAreaSize));

        _busList = new ReorderableList(_so, _busesProp, true, true, true, true)
        {
            drawHeaderCallback = r => EditorGUI.LabelField(r, "Bus Queue (front first)"),
            drawElementCallback = (r, i, _, _) =>
            {
                var elem = _busesProp.GetArrayElementAtIndex(i);
                elem.enumValueIndex = (int)(PassengerColor)EditorGUI.EnumPopup(r, (PassengerColor)elem.enumValueIndex);
            }
        };
    }

    private void DrawGrid()
    {
        _so.Update();

        var rows = Mathf.Max(1, _rowsProp.intValue);
        var cols = Mathf.Max(1, _colsProp.intValue);
        const float btn = 22f;

        for (var r = 0; r < rows; r++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            for (var c = 0; c < cols; c++)
            {
                var idx = r * cols + c;
                if (idx >= _cellsProp.arraySize) continue;

                var cellProp = _cellsProp.GetArrayElementAtIndex(idx);

                var typeProp = cellProp.FindPropertyRelative(nameof(Cell.type));
                var colourProp = cellProp.FindPropertyRelative(nameof(Cell.colour));

                var type = (CellType)typeProp.enumValueIndex;
                var colour = (PassengerColor)colourProp.enumValueIndex;

                GUI.backgroundColor = ColorFor(type, colour);

                if (GUILayout.Button(GUIContent.none, GUILayout.Width(btn), GUILayout.Height(btn)))
                {
                    Undo.RecordObject(_level, "Paint Cell");

                    if (Event.current.button == 1)
                    {
                        typeProp.enumValueIndex = (int)CellType.Void;
                        colourProp.enumValueIndex = 0;
                    }
                    else
                    {
                        typeProp.enumValueIndex = (int)_brush.type;
                        colourProp.enumValueIndex = (int)_brush.colour;
                    }
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;
        _so.ApplyModifiedProperties();
    }

    private void DrawPalette()
    {
        GUILayout.Label("Brush");

        EditorGUILayout.BeginHorizontal();
        DrawBrush(CellType.Empty, PassengerColor.Red, "Empty");
        DrawBrush(CellType.Void, PassengerColor.Red, "Void");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        foreach (var kv in ColorOf)
            DrawBrush(CellType.Passenger, kv.Key, kv.Key.ToString());
        EditorGUILayout.EndHorizontal();
    }

    private void DrawBrush(CellType type, PassengerColor col, string label)
    {
        GUI.backgroundColor = ColorFor(type, col);

        var pick = GUILayout.Toggle(
            _brush.type == type && _brush.colour == col,
            label, "Button", GUILayout.Height(24));

        if (pick)
        {
            _brush.type = type;
            _brush.colour = col;
        }

        GUI.backgroundColor = Color.white;
    }

    private void DrawBusQueue()
    {
        _so.Update();
        _busList?.DoLayoutList();
        _so.ApplyModifiedProperties();
    }

    private static Color ColorFor(CellType type, PassengerColor id)
    {
        return type switch
        {
            CellType.Empty => Color.white,
            CellType.Void => VoidGrey,
            CellType.Passenger => ColorOf[id],
            _ => Color.magenta
        };
    }
}
#endif