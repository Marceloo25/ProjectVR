using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(DeckScript))]
public class DeckScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DeckScript deck = (DeckScript)target;
        EditorGUILayout.Space();
        GUILayout.Label("Deck Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Collect Children"))
        {
            Undo.RecordObject(deck, "Collect Children");
            deck.CollectChildren();
        }

        if (GUILayout.Button("Shuffle Deck"))
        {
            Undo.RecordObject(deck, "Shuffle Deck");
            deck.Shuffle();
        }

        if (GUILayout.Button("Draw Card"))
        {
            Undo.RecordObject(deck, "Draw Card");
            deck.Draw();

            // If a card was drawn, select and ping it so user sees it in Hierarchy/Scene
            //if (deck.lastDrawnCard != null)
            //{
            //    Selection.activeGameObject = deck.lastDrawnCard;
            //    EditorGUIUtility.PingObject(deck.lastDrawnCard);
            //    if (SceneView.lastActiveSceneView != null)
            //        SceneView.lastActiveSceneView.FrameSelected();
            //}
        }

        if (GUILayout.Button("Reset Deck"))
        {
            Undo.RecordObject(deck, "Reset Deck");
            deck.ResetDeck();
        }

        if (GUILayout.Button("Shuffle And Draw One"))
        {
            Undo.RecordObject(deck, "Shuffle And Draw One");
            deck.ShuffleAndDrawOne();

            if (deck.lastDrawnCard != null)
            {
                Selection.activeGameObject = deck.lastDrawnCard;
                EditorGUIUtility.PingObject(deck.lastDrawnCard);
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.FrameSelected();
            }
        }
    }
}
