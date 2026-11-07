using Godot;

public partial class Spawner : Node2D
{
    [Export] public PackedScene SpawnableScene { get; set; } = null!;
    [Export] public PackedScene StopSignHazardScene { get; set; } = null!;
    [Export] public PackedScene BoostPowerupScene { get; set; } = null!;
    [Export] public PackedScene ScoreMultiplierPowerupScene { get; set; } = null!;
    [Export] public PackedScene MaxAirPowerupScene { get; set; } = null!;
    [Export] public float SpawnInterval { get; set; } = 1.7f;
    [Export] public float HazardIntervalMin { get; set; } = 4.5f;
    [Export] public float HazardIntervalMax { get; set; } = 6.0f;
    [Export] public float HazardStartDelay { get; set; } = 18.0f;
    [Export] public float MinimumSpawnSeparation { get; set; } = 220.0f;
    [Export] public float PowerupStartDelay { get; set; } = 6.0f;
    [Export] public float PowerupIntervalMin { get; set; } = 8.0f;
    [Export] public float PowerupIntervalMax { get; set; } = 12.0f;

    private Timer spawnTimer = null!;
    private Timer hazardTimer = null!;
    private Timer powerupTimer = null!;
    private Node2D spawnablesContainer = null!;
    private readonly SpawnPosition spawnPosition = new();

    public override void _Ready()
    {
        spawnTimer = GetNode<Timer>("SpawnTimer");
        hazardTimer = GetNode<Timer>("HazardTimer");
        powerupTimer = GetNode<Timer>("PowerupTimer");
        spawnablesContainer = GetNode<Node2D>("../Spawnables");

        spawnTimer.WaitTime = SpawnInterval;
        spawnTimer.Timeout += SpawnRing;
        hazardTimer.OneShot = true;
        hazardTimer.Timeout += SpawnHazard;
        powerupTimer.OneShot = true;
        powerupTimer.Timeout += SpawnPowerup;
        spawnTimer.Start(SpawnInterval);
        StartHazardTimer();
        powerupTimer.Start(PowerupStartDelay);
    }

    private void SpawnRing()
    {
        if (Spawn(SpawnableScene))
            spawnTimer.Start(GetRingInterval());
        else
            spawnTimer.Start(0.35f);
    }

    private void SpawnHazard()
    {
        foreach (Node child in spawnablesContainer.GetChildren())
        {
            if (child is FlyingHazard activeHazard && !activeHazard.IsQueuedForDeletion())
            {
                hazardTimer.Start(0.5f);
                return;
            }
        }

        if (Spawn(StopSignHazardScene))
            StartHazardTimer();
        else
            hazardTimer.Start(0.5f);
    }

    private void SpawnPowerup()
    {
        PackedScene scene;
        if (AirMeter.Instance?.CanSpawnMaxAirPowerup() == true && GD.Randf() < 0.35f)
            scene = MaxAirPowerupScene;
        else if (GD.Randf() < 0.5f)
            scene = BoostPowerupScene;
        else
            scene = ScoreMultiplierPowerupScene;

        if (Spawn(scene))
            StartPowerupTimer();
        else
            powerupTimer.Start(0.5f);
    }

    private void StartPowerupTimer()
    {
        powerupTimer.Start((float)GD.RandRange(PowerupIntervalMin, PowerupIntervalMax));
    }

    private void StartHazardTimer()
    {
        float flightTime = AirMeter.Instance?.GetFlightTime() ?? 0f;
        if (flightTime < HazardStartDelay)
        {
            hazardTimer.Start(HazardStartDelay - flightTime);
            return;
        }

        float obstacleDifficulty = Mathf.Clamp((flightTime - HazardStartDelay) / 42f, 0f, 1f);
        float minimumInterval = Mathf.Lerp(HazardIntervalMin, HazardIntervalMin * 0.45f, obstacleDifficulty);
        float maximumInterval = Mathf.Lerp(HazardIntervalMax, HazardIntervalMax * 0.45f, obstacleDifficulty);
        hazardTimer.Start((float)GD.RandRange(minimumInterval, maximumInterval));
    }

    public void RefreshSpawnRates()
    {
        spawnTimer?.Start(GetRingInterval());
    }

    private float GetRingInterval()
    {
        float baseInterval = Mathf.Lerp(SpawnInterval, SpawnInterval * 0.65f, GetDifficulty());
        float boostMultiplier = AirMeter.Instance?.GetBoostMultiplier() ?? 1f; 
        return baseInterval / boostMultiplier;
    }

    private float GetDifficulty() 
    { 
        return AirMeter.Instance?.GetDifficulty() ?? 0f; 
    }

    private bool Spawn(PackedScene scene)
    {
        if (scene == null)
            return false;

        Node2D spawnable = scene.Instantiate<Node2D>();
        Rect2 worldViewport = GetWorldViewportRect();
        if (!spawnPosition.TryFindPosition(spawnable, worldViewport, spawnablesContainer,
                MinimumSpawnSeparation, out Vector2 position))
        {
            spawnable.Free();
            return false;
        }

        spawnablesContainer.AddChild(spawnable);
        if (spawnable is FlyingHazard hazard)
            hazard.SetSpawnPosition(position);
        else
            spawnable.GlobalPosition = position;

        return true;
    }

    private Rect2 GetWorldViewportRect()
    {
        Rect2 screenRect = GetViewport().GetVisibleRect();
        Transform2D screenToWorld = GetViewport().GetCanvasTransform().AffineInverse();
        Vector2 topLeft = screenToWorld * screenRect.Position;
        Vector2 bottomRight = screenToWorld * screenRect.End;
        return new Rect2(topLeft, bottomRight - topLeft);
    }
}
