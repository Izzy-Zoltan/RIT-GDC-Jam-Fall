using Godot;

public partial class PowerupEffects : Node
{
    public float BoostRemaining { get; private set; }
    public float BoostDuration { get; private set; }
    public float ScoreMultiplierRemaining { get; private set; }
    public float ScoreMultiplierDuration { get; private set; }
    public bool IsBoostActive => BoostRemaining > 0f;
    public bool IsScoreMultiplierActive => ScoreMultiplierRemaining > 0f;

    public void ActivateBoost(float duration)
    {
        BoostRemaining = Mathf.Max(BoostRemaining, duration);
        BoostDuration = Mathf.Max(BoostDuration, duration);
    }

    public void ActivateScoreMultiplier(float duration)
    {
        ScoreMultiplierRemaining = Mathf.Max(ScoreMultiplierRemaining, duration);
        ScoreMultiplierDuration = Mathf.Max(ScoreMultiplierDuration, duration);
    }

    public bool Advance(float deltaSeconds)
    {
        bool boostWasActive = IsBoostActive;
        BoostRemaining = Mathf.Max(0f, BoostRemaining - deltaSeconds);
        ScoreMultiplierRemaining = Mathf.Max(0f, ScoreMultiplierRemaining - deltaSeconds);
        return boostWasActive && !IsBoostActive;
    }

    public void ResetEffects()
    {
        BoostRemaining = BoostDuration = 0f;
        ScoreMultiplierRemaining = ScoreMultiplierDuration = 0f;
    }
}
