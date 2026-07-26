using UnityEngine;

// Carries state between scenes. Nothing else in the project survives a scene
// load, so shamrocks picked up in Level 1 are banked here and read back as
// starting ammo when MainScene spawns the player.
public static class RunProgress
{
    public static int CarriedShamrocks { get; private set; }
    public static int Level1Collected { get; private set; }
    public static bool ClearedLevel1 { get; private set; }

    // Play Mode can be entered with domain reload disabled, which would leave
    // last session's totals sitting in these fields. Clear them on load.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetRun()
    {
        CarriedShamrocks = 0;
        Level1Collected = 0;
        ClearedLevel1 = false;
    }

    // Called when the player reaches the Level 1 beanstalk.
    public static void BankLevel1(int shamrocksHeld)
    {
        Level1Collected = Mathf.Max(0, shamrocksHeld);
        CarriedShamrocks = Level1Collected;
        ClearedLevel1 = true;

        Debug.Log($"[RunProgress] Banked {CarriedShamrocks} shamrock(s) from Level 1.");
    }

    // Deliberately non-consuming: restarting MainScene should not wipe the ammo
    // the player earned in Level 1.
    public static int GetCarriedShamrocks()
    {
        return CarriedShamrocks;
    }
}
