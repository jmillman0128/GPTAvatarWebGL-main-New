#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Pre-build safety check: fails the build immediately if dev-keys.txt is still present
/// in Assets/Resources, preventing API keys from being accidentally bundled into a release.
///
/// Delete Assets/Resources/dev-keys.txt (and its .meta) before building for distribution.
/// </summary>
public class PreBuildKeyCheck : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        TextAsset devKeysAsset = Resources.Load<TextAsset>("dev-keys");
        if (devKeysAsset != null)
        {
            throw new BuildFailedException(
                "[PreBuildKeyCheck] BUILD BLOCKED: Assets/Resources/dev-keys.txt is present. " +
                "Delete this file and its .meta before building to prevent API keys from " +
                "being bundled into the release APK.");
        }
    }
}
#endif
