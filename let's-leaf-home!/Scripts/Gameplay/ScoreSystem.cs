using Godot;

public sealed class ScoreSystem
{
    private float scoreProgress;

    public float FlightTime { get; private set; }
    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int MaximumCombo { get; private set; }

    public void Advance(float deltaSeconds, float speedMultiplier, float scoreMultiplier)
    {
        FlightTime += deltaSeconds;
        scoreProgress += deltaSeconds * speedMultiplier * scoreMultiplier * 100f;
        Score = (int)scoreProgress;
    }

    public void AddCombo()
    {
        Combo++;
        MaximumCombo = Mathf.Max(MaximumCombo, Combo);
    }

    public void ResetCombo() { 
        Combo = 0; 
    }
    public float GetDifficulty() { 
        return Mathf.Clamp(FlightTime / 45f, 0f, 1f); 
    }
}
