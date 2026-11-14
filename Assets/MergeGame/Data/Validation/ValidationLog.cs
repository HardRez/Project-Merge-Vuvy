using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MergeGame.Data.Validation
{
    /// <summary>Surfaces validation problems in the Console with the asset as context, so clicking a message selects it.</summary>
    internal static class ValidationLog
    {
        public static void Report(Object asset, IReadOnlyList<string> problems, bool reportSuccess)
        {
            foreach (var problem in problems)
                Debug.LogWarning($"{asset.name}: {problem}", asset);

            if (reportSuccess && problems.Count == 0)
                Debug.Log($"{asset.name}: no problems found.", asset);
        }

        /// <summary>
        /// Only saved assets are checked: in-memory instances (tests, the seeder while filling an asset) are validated
        /// explicitly by their owner and would otherwise log noise for every half-built state. Never throws.
        /// </summary>
        public static void ReportFromOnValidate(Object asset, Func<IReadOnlyList<string>> validate)
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorUtility.IsPersistent(asset))
                return;
#endif
            try
            {
                Report(asset, validate(), reportSuccess: false);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{asset.name}: validation failed unexpectedly: {exception}", asset);
            }
        }
    }
}
