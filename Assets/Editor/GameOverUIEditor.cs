using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameOverUI))]
public class GameOverUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Panel de Pruebas (Editor)", EditorStyles.boldLabel);

        GameOverUI ui = (GameOverUI)target;

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Haz clic en los botones para probar la pantalla directamente en la ventana de juego:", MessageType.Info);

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(0.25f, 0.85f, 0.45f);
            if (GUILayout.Button("▶ Probar Victoria: Tiempo Sobrevivido", GUILayout.Height(30)))
            {
                ui.Show(MatchResultReason.SurviveTime, 14, 0);
            }
            if (GUILayout.Button("▶ Probar Victoria: Enemigos Eliminados", GUILayout.Height(30)))
            {
                ui.Show(MatchResultReason.DefeatedAllEnemies, 25, 1);
            }

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(0.92f, 0.28f, 0.28f);
            if (GUILayout.Button("▶ Probar Derrota: Sin Vidas", GUILayout.Height(30)))
            {
                ui.Show(MatchResultReason.OutOfLives, 8, 3);
            }
            if (GUILayout.Button("▶ Probar Derrota: Torre Destruida", GUILayout.Height(30)))
            {
                ui.Show(MatchResultReason.TowerDestroyed, 11, 2);
            }

            EditorGUILayout.Space(6);
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("✕ Ocultar Pantalla", GUILayout.Height(28)))
            {
                ui.Hide();
            }

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.7f, 0.8f, 1f);
            if (GUILayout.Button("🔄 Reconstruir UI Limpia", GUILayout.Height(28)))
            {
                ui.RebuildUI();
                ui.Show(MatchResultReason.SurviveTime, 18);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Inicia el Modo Play (▶) para activar los botones de prueba interactivos aquí en el Inspector.", MessageType.None);
        }
    }
}
