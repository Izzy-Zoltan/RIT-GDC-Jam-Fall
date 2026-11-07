using Godot;

public partial class AirMeter : Node
{
    public static AirMeter Instance { get; private set; } = null!;

    [Signal] public delegate void AirChangedEventHandler(float currentAir, float maximumAir, bool flash);
    [Signal] public delegate void ScoreChangedEventHandler(int score);
    [Signal] public delegate void ComboChangedEventHandler(int combo, bool playPop);
    [Signal] public delegate void PowerupEffectsChangedEventHandler(
        float boostRemaining, float boostDuration, float scoreRemaining, float scoreDuration);
    [Signal] public delegate void GameOverEventHandler(int score, int maximumCombo);

    [Export] public float DrainPerSecond { get; set; } = 8f;
    [Export] public float MaximumAir { get; set; } = 100f;
    [Export] public float DrainPauseAfterRefill { get; set; } = 0.8f;
    [Export] public float ForwardBoostMultiplier { get; set; } = 2.5f;

    private Camera2D camera = null!;
    private PlayerControl player = null!;
    private PowerupEffects powerupEffects = null!;
    private readonly ScoreSystem scoreSystem = new();
    private float air, refillRemaining, refillRate, drainPauseRemaining, comboGraceRemaining;
    private float boostTimeRemaining, boostZoom = 1f;
    private bool gameOver, gameEnding;

    public int CurrentScore => scoreSystem.Score;
    public int CurrentCombo => scoreSystem.Combo;
    public int MaximumCombo => scoreSystem.MaximumCombo;
    public float CurrentAir => air;
    public PowerupEffects Effects => powerupEffects;

    public override void _Ready()
    {
        Instance = this;
        AddToGroup("airmeter");
        camera = GetNode<Camera2D>("../../Camera2D");
        player = GetTree().GetFirstNodeInGroup("player") as PlayerControl;
        powerupEffects = GetNode<PowerupEffects>("PowerupEffects");
        air = MaximumAir;
        drainPauseRemaining = DrainPauseAfterRefill;
        EmitSignal(SignalName.AirChanged, air, MaximumAir, false);
        EmitSignal(SignalName.ScoreChanged, scoreSystem.Score);
        EmitSignal(SignalName.ComboChanged, scoreSystem.Combo, false);
        EmitPowerupEffectsChanged();
    }

    public override void _ExitTree()
    {
        if (Instance == this)
            Instance = null!;
    }

    public override void _Process(double delta)
    {
        float deltaSeconds = (float)delta;

        float targetZoom = 1f;
        if (IsForwardBoostActive()) targetZoom = 1.08f;

        boostZoom = Mathf.Lerp(boostZoom, targetZoom, 1f - Mathf.Exp(-5f * deltaSeconds));
        camera.Zoom = Vector2.One * boostZoom;

        if (!gameOver && boostTimeRemaining > 0f)
        {
            boostTimeRemaining = Mathf.Max(0f, boostTimeRemaining - deltaSeconds);
            if (boostTimeRemaining <= 0f) RefreshSpawnRates();
        }

        if (!gameOver)
        {
            if (powerupEffects.Advance(deltaSeconds))
                comboGraceRemaining = Mathf.Max(comboGraceRemaining, 0.5f);
            comboGraceRemaining = Mathf.Max(0f, comboGraceRemaining - deltaSeconds);

            float refillThisFrame = Mathf.Min(refillRemaining, refillRate * deltaSeconds);
            refillRemaining -= refillThisFrame;
            float drainTime = deltaSeconds;
            if (drainPauseRemaining > 0f)
            {
                float pausedTime = Mathf.Min(drainPauseRemaining, drainTime);
                drainPauseRemaining -= pausedTime;
                drainTime -= pausedTime;
            }
            if (IsForwardBoostActive()) drainTime = 0f;
            air = Mathf.Clamp(air + refillThisFrame - DrainPerSecond * drainTime, 0f, MaximumAir);
        }

        if (air > 0f)
        {
            float scoreMultiplier = powerupEffects.IsScoreMultiplierActive ? 2f : 1f;
            scoreSystem.Advance(deltaSeconds, GetSpeedMultiplier(), scoreMultiplier);
        }

        EmitSignal(SignalName.AirChanged, air, MaximumAir, false);
        EmitSignal(SignalName.ScoreChanged, scoreSystem.Score);
        EmitPowerupEffectsChanged();
        player?.SetAirDepleted(air <= 0f);
        if (air <= 0f && !gameEnding) BeginGameOverFall();
    }

    public void Refill(float amount)
    {
        float available = Mathf.Max(0f, MaximumAir - air - refillRemaining);
        float accepted = Mathf.Min(amount, available);
        if (accepted > 0f) drainPauseRemaining = DrainPauseAfterRefill;
        float immediateRefill = accepted * 0.7f;
        air += immediateRefill;
        refillRemaining += accepted - immediateRefill;
        refillRate = refillRemaining / 0.25f;
        EmitSignal(SignalName.AirChanged, air, MaximumAir, accepted > 0f);
    }

    public void AddCombo()
    {
        scoreSystem.AddCombo();
        boostTimeRemaining = 1f;
        EmitSignal(SignalName.ComboChanged, scoreSystem.Combo, true);
        RefreshSpawnRates();
    }

    public void ActivateForwardBoost(float duration)
    {
        powerupEffects.ActivateBoost(duration);
        comboGraceRemaining = Mathf.Max(comboGraceRemaining, duration + 0.5f);
        EmitPowerupEffectsChanged();
        RefreshSpawnRates();
    }

    public void ActivateScoreMultiplier(float duration)
    {
        powerupEffects.ActivateScoreMultiplier(duration);
        EmitPowerupEffectsChanged();
    }

    public void RefillToMaximum()
    {
        air = MaximumAir;
        refillRemaining = refillRate = 0f;
        drainPauseRemaining = DrainPauseAfterRefill;
        EmitSignal(SignalName.AirChanged, air, MaximumAir, true);
    }

    public bool IsForwardBoostActive() 
    { 
        return powerupEffects.IsBoostActive;
    }
    public bool CanSpawnMaxAirPowerup() 
    {
        return air < MaximumAir * 0.5f; 
    }

    public void MissRing()
    {
        if (IsForwardBoostActive() || comboGraceRemaining > 0f) return;
        scoreSystem.ResetCombo();
        EmitSignal(SignalName.ComboChanged, scoreSystem.Combo, false);
        RefreshSpawnRates();
    }

    public float GetDifficulty()
    { 
        return scoreSystem.GetDifficulty(); 
    }

    public float GetBoostMultiplier()
    {
        if (boostTimeRemaining > 0f) return 1.12f;
        return 1f;
    }

    public float GetFlightTime()
    {
        return scoreSystem.FlightTime;
    }

    public float GetSpeedMultiplier()
    {
        
        float powerBoostMultiplier =  1f;
        if (IsForwardBoostActive()) powerBoostMultiplier = ForwardBoostMultiplier;
        return Mathf.Lerp(1f, 1.67f, GetDifficulty()) * GetBoostMultiplier() * powerBoostMultiplier;
    }

    public void HitObstacle()
    {
        if (gameEnding) return;
        air = refillRemaining = 0f;
        EmitSignal(SignalName.AirChanged, air, MaximumAir, false);
        BeginGameOverFall();
    }

    public void PlayerFellOutOfScreen()
    {
        if (gameEnding && !gameOver)
        {
            gameOver = true;
            EmitSignal(SignalName.GameOver, scoreSystem.Score, scoreSystem.MaximumCombo);
        }
    }

    private void BeginGameOverFall()
    {
        gameEnding = true;
        player?.SetAirDepleted(true);
        GetNode<Timer>("../../Spawnable Spawner/SpawnTimer").Stop();
        GetNode<Timer>("../../Spawnable Spawner/HazardTimer").Stop();
        GetNode<Timer>("../../Spawnable Spawner/PowerupTimer").Stop();
        Node spawner = GetNode<Node>("../../Spawnable Spawner");
        spawner.SetProcess(false);
        spawner.SetPhysicsProcess(false);
        foreach (Node spawnable in GetNode<Node2D>("../../Spawnables").GetChildren())
            spawnable.SetProcess(false);
    }

    private void EmitPowerupEffectsChanged()
    {
        EmitSignal(SignalName.PowerupEffectsChanged,
        powerupEffects.BoostRemaining, powerupEffects.BoostDuration,
        powerupEffects.ScoreMultiplierRemaining, powerupEffects.ScoreMultiplierDuration);
    }
    private void RefreshSpawnRates() 
    {
        GetNodeOrNull<Spawner>("../../Spawnable Spawner")?.RefreshSpawnRates();
    }
}
