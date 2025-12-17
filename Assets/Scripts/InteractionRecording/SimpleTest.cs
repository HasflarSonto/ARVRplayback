using UnityEngine;

/// <summary>
/// Simple test to verify console is working
/// </summary>
public class SimpleTest : MonoBehaviour
{
    [ContextMenu("Test Console Output")]
    public void TestConsole()
    {
        Debug.Log("✅ Console is working!");
        Debug.LogWarning("⚠️ This is a warning");
        Debug.LogError("❌ This is an error");
        Debug.Log("If you see these 3 messages, console is working correctly!");
    }
}
