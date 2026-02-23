using Game.Features.Law;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LawConfig))]
public class LawConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        // Reference to the target ScriptableObject (LawConfig)
        LawConfig config = (LawConfig)target;

        GUILayout.Space(20);

        // Creating a green button for importing data
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Import from TSV", GUILayout.Height(40)))
        {
            ImportData(config);
        }
        GUI.backgroundColor = Color.white;
    }

    private void ImportData(LawConfig config)
    {
        // Show open file panel (.tsv)
        string path = EditorUtility.OpenFilePanel("Select the file with laws", "", "tsv");

        if (string.IsNullOrEmpty(path))
            return; // User clolsed the window

        // Read all lines from the selected file
        string[] lines = File.ReadAllLines(path);

        if (lines.Length == 0)
        {
            Debug.LogWarning("File is empty!");
            return;
        }

        var importedLaws = new List<LawData>();
        char separator = '\t';

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] row = line.Split(separator);

            try
            {
                // Parsing the row into a LawData object using the LawDataParser
                LawData newLaw = LawDataParser.ParseRow(row);

                if (newLaw != null)
                {
                    importedLaws.Add(newLaw);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Parsing failed in row {i}! (Law: {row[0]}): {e.Message}");
            }
        }

        // Write data to the ScriptableObject
        config.SetLawsFromImport(importedLaws);

        // Save the changes on the hard drive
        AssetDatabase.SaveAssets();

        Debug.Log($"<color=green>Successfully imported {importedLaws.Count} Laws!</color>");
    }
}