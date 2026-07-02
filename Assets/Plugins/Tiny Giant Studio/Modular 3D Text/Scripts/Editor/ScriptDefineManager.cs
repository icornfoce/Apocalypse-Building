#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEditor;

namespace MText
{
    internal static class ScriptDefineManager
    {
        readonly private static string mTextDefine = "MODULAR_3D_TEXT";

        [InitializeOnLoadMethod]
        private static void AddScriptDefine()
        {
            BuildTargetGroup currentTarget = EditorUserBuildSettings.selectedBuildTargetGroup;

            if (currentTarget == BuildTargetGroup.Unknown)
                return;

#if UNITY_2023_1_OR_NEWER
            var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(currentTarget);
            string scriptDefinesString = PlayerSettings.GetScriptingDefineSymbols(namedTarget).Trim();
#else
            string scriptDefinesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(currentTarget).Trim();
#endif
            string[] scriptDefines = scriptDefinesString.Split(';');

            if (scriptDefines.Contains(mTextDefine))
                return;

            //This shouldn't be needed for 1 symbol but an existing third party tool was causing issue or this is really needed and i dont understand how this works
            if (scriptDefinesString.EndsWith(";", StringComparison.InvariantCulture) == false)
            {
                scriptDefinesString += ";";
            }

            scriptDefinesString += mTextDefine;

#if UNITY_2023_1_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, scriptDefinesString);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(currentTarget, scriptDefinesString);
#endif
        }
    }
}
#endif