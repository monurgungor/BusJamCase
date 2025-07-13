using BusJam.Core;
using BusJam.Data;
using UnityEditor;
using UnityEngine;

namespace BusJam.Editor
{
    [CustomEditor(typeof(GameConfig))]
    public class GameConfigEditor : UnityEditor.Editor
    {
        private GameConfig _gameConfig;
        private bool _showGridConfig = true;
        private bool _showStationConfig = true;
        private bool _showAnimationConfig = true;
        private bool _showBusConfig = true;
        private bool _showColorConfig = true;
        private bool _showPassengerConfig = true;

        private void OnEnable()
        {
            _gameConfig = (GameConfig)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Game Configuration", EditorStyles.largeLabel);
            EditorGUILayout.Space();

            _showGridConfig = EditorGUILayout.Foldout(_showGridConfig, "Grid Configuration", true);
            if (_showGridConfig) DrawGridConfig();

            _showStationConfig = EditorGUILayout.Foldout(_showStationConfig, "Station Configuration", true);
            if (_showStationConfig) DrawStationConfig();

            _showBusConfig = EditorGUILayout.Foldout(_showBusConfig, "Bus Configuration", true);
            if (_showBusConfig) DrawBusConfig();

            _showPassengerConfig = EditorGUILayout.Foldout(_showPassengerConfig, "Passenger Configuration", true);
            if (_showPassengerConfig) DrawPassengerConfig();

            _showColorConfig = EditorGUILayout.Foldout(_showColorConfig, "Color Configuration", true);
            if (_showColorConfig) DrawColorConfig();

            _showAnimationConfig = EditorGUILayout.Foldout(_showAnimationConfig, "Animation Configuration", true);
            if (_showAnimationConfig) DrawAnimationConfig();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGridConfig()
        {
            EditorGUILayout.BeginVertical("box");

            var cellSizeProp = serializedObject.FindProperty("cellSize");
            cellSizeProp.floatValue = EditorGUILayout.Slider("Cell Size", cellSizeProp.floatValue, 0.5f, 3f);

            EditorGUILayout.EndVertical();
        }

        private void DrawStationConfig()
        {
            EditorGUILayout.BeginVertical("box");

            var stationSlotSpacingProp = serializedObject.FindProperty("stationSlotSpacing");
            stationSlotSpacingProp.floatValue =
                EditorGUILayout.Slider("Station Slot Spacing", stationSlotSpacingProp.floatValue, 0.5f, 3f);

            EditorGUILayout.EndVertical();
        }

        private void DrawBusConfig()
        {
            EditorGUILayout.BeginVertical("box");

            var arrivalSpeedProp = serializedObject.FindProperty("busArrivalSpeed");
            arrivalSpeedProp.floatValue = EditorGUILayout.Slider("Arrival Speed", arrivalSpeedProp.floatValue, 1f, 15f);

            EditorGUILayout.EndVertical();
        }

        private void DrawPassengerConfig()
        {
            EditorGUILayout.BeginVertical("box");

            var moveSpeedProp = serializedObject.FindProperty("passengerMoveSpeed");

            moveSpeedProp.floatValue = EditorGUILayout.Slider("Move Speed", moveSpeedProp.floatValue, 1f, 10f);
            EditorGUILayout.EndVertical();
        }

        private void DrawColorConfig()
        {
            EditorGUILayout.BeginVertical("box");

            var gameColorsProp = serializedObject.FindProperty("gameColors");

            EditorGUILayout.LabelField("Passenger Colors", EditorStyles.boldLabel);

            for (var i = 0; i < gameColorsProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                var colorEnum = (PassengerColor)i;
                EditorGUILayout.LabelField(colorEnum.ToString(), GUILayout.Width(80));

                var colorProp = gameColorsProp.GetArrayElementAtIndex(i);
                var newColor = EditorGUILayout.ColorField(colorProp.colorValue);
                colorProp.colorValue = newColor;

                var colorRect = GUILayoutUtility.GetRect(50, 20);
                EditorGUI.DrawRect(colorRect, newColor);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            EditorGUILayout.EndVertical();
        }

        private void DrawAnimationConfig()
        {
            EditorGUILayout.BeginVertical("box");

            var errorDurationProp = serializedObject.FindProperty("errorAnimationDuration");
            errorDurationProp.floatValue =
                EditorGUILayout.Slider("Error Animation", errorDurationProp.floatValue, 0.1f, 1f);
            
            EditorGUILayout.EndVertical();
        }
    }
}