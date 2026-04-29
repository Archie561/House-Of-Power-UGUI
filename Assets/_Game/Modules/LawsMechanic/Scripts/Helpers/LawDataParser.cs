using Game.General;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Features.Law
{
    /// <summary>
    /// Static helper class responsible for parsing CSV rows from external table into LawData objects.
    /// </summary>
    public static class LawDataParser
    {
        // ID, Type, 3 Accept pairs, 3 Reject pairs
        private const int EXPECTED_COLS = 14;

        // column indexes according to the table
        private const int COL_ID = 0;
        private const int COL_TYPE = 1;

        // Accept columns: Resource, Value, Resource, Value...
        private static readonly int[] ACCEPT_RES_COLS = { 2, 4, 6 };
        private static readonly int[] ACCEPT_VAL_COLS = { 3, 5, 7 };

        // Reject columns: Resource, Value, Resource, Value...
        private static readonly int[] REJECT_RES_COLS = { 8, 10, 12 };
        private static readonly int[] REJECT_VAL_COLS = { 9, 11, 13 };

        public static LawData ParseRow(string[] csvRow)
        {
            // If the row is null or has insufficient columns, we can't parse it
            if (csvRow == null || csvRow.Length < EXPECTED_COLS)
            {
                Debug.LogError("Error: CSV row is null or does not have enough columns to parse a law.");
                return null;
            }

            string id = csvRow[COL_ID];

            if (!Enum.TryParse(csvRow[COL_TYPE], true, out DocumentType type))
            {
                Debug.LogError($"Error: Unknown document type '{csvRow[COL_TYPE]}' in law {id}");
                return null;
            }

            List<ResourceAmount> onAccept = ParseSlots(csvRow, ACCEPT_RES_COLS, ACCEPT_VAL_COLS, id);
            List<ResourceAmount> onReject = ParseSlots(csvRow, REJECT_RES_COLS, REJECT_VAL_COLS, id);

            return new LawData(id, type, onAccept, onReject);
        }

        private static List<ResourceAmount> ParseSlots(string[] row, int[] resCols, int[] valCols, string lawId)
        {
            var effects = new List<ResourceAmount>();

            for (int i = 0; i < resCols.Length; i++)
            {
                string resString = row[resCols[i]].Trim();
                string valString = row[valCols[i]].Trim();

                // If resource or value column is empty, skip this slot
                if (string.IsNullOrEmpty(resString) || string.IsNullOrEmpty(valString)) continue;

                // Parsing the resource type
                if (!Enum.TryParse(resString, true, out ResourceType resType))
                {
                    Debug.LogError($"Error: Unknown resource type '{resString}' in law {lawId}");
                    continue;
                }

                // Parsing the value
                if (!int.TryParse(valString, out int amount))
                {
                    Debug.LogError($"Error: value '{valString}' is not an integer in law {lawId}");
                    continue;
                }

                effects.Add(new ResourceAmount(resType, amount));
            }

            return effects;
        }
    }
}