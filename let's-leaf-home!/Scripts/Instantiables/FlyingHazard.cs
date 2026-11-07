using Godot;

public partial class FlyingHazard : Area2D
{
    [Export] public float RadiusMin { get; set; } = 60.0f;
    [Export] public float RadiusMax { get; set; } = 75.0f;
    [Export] public float WarningDuration { get; set; } = 1.2f;
    [Export] public float PopDuration { get; set; } = 0.24f;
    [Export] public float HoldDuration { get; set; } = 0.9f;

    private CollisionShape2D collisionShape = null;
    private CollisionShape2D poleCollision = null;
    private Sprite2D sprite = null;
    private Sprite2D highlight = null;
    private Label warningLabel = null;
    private ShaderMaterial highlightMaterial = null;
    private AirMeter airMeter = null;

    private float radius;
    private float elapsed;
    private float popupScale;
    private float poleReach;
    private Vector2 edgeAnchor;
    private Vector2 poleDirection = Vector2.Down;
    private bool hasEdgeAnchor;
    private bool hasHitPlayer;
    private bool collisionActive;
    private bool showWarning;
    private bool poleDirectionConfigured;

    public void ConfigurePoleDirection(Vector2 direction)
    {
        if (direction.LengthSquared() < 0.0001f)
        {
            GD.PushWarning("FlyingHazard received a zero-length pole direction.");
            direction = Vector2.Down;
        }

        poleDirection = direction.Normalized();
        Rotation = poleDirection.Angle() - Vector2.Down.Angle();
        poleDirectionConfigured = true;
    }

    public void SetSpawnPosition(Vector2 position)
    {
        edgeAnchor = position;
        hasEdgeAnchor = true;
        GlobalPosition = position;
    }

    public override void _Ready()
    {
        if (!hasEdgeAnchor)
        {
            edgeAnchor = GlobalPosition;
            hasEdgeAnchor = true;
        }

        if (!poleDirectionConfigured)
            poleDirection = Vector2.Down.Rotated(Rotation).Normalized();

        CollisionLayer = 0;
        CollisionMask = 1;

        sprite = GetNode<Sprite2D>("Sprite2D");

        highlightMaterial = new ShaderMaterial
        {
            Shader = GD.Load<Shader>("res://assets/highlight_outline.gdshader")
        };

        highlight = new Sprite2D
        {
            Texture = sprite.Texture,
            Centered = sprite.Centered,
            Offset = sprite.Offset,
            ZIndex = sprite.ZIndex - 1,
            Material = highlightMaterial
        };
        highlightMaterial.SetShaderParameter("outline_width", 6.0f);
        AddChild(highlight);

        airMeter = GetTree().GetFirstNodeInGroup("airmeter") as AirMeter;
        GetNode<SoundEffects>("/root/SoundEffects").PlayWarning();

        collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        poleCollision = GetNode<CollisionShape2D>("PoleCollisionShape2D");

        collisionShape.SetDeferred("disabled", true);
        poleCollision.SetDeferred("disabled", true);

        radius = (float)GD.RandRange(RadiusMin, RadiusMax);

        float radiusScale = radius / RadiusMax;
        float signScale = (radius * 2.0f) / 180.0f;

        collisionShape.Scale = Vector2.One * radiusScale;
        poleCollision.Scale = Vector2.One * radiusScale;

        poleReach = Mathf.Min(730.0f * signScale, 250.0f);
        poleCollision.Position = new Vector2(0.0f, 341.7f);

        sprite.RegionEnabled = false;
        sprite.Scale = Vector2.One * signScale;
        sprite.Offset = new Vector2(0.0f, 280.0f);

        highlight.Scale = sprite.Scale;
        highlight.Offset = sprite.Offset;
        highlight.RegionEnabled = false;

        warningLabel = new Label
        {
            Text = "!!",
            TopLevel = true,
            Size = new Vector2(72.0f, 48.0f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ZIndex = 10
        };
        warningLabel.AddThemeFontOverride("font", GD.Load<Font>("res://assets/Tiny5-Regular.ttf"));
        warningLabel.AddThemeFontSizeOverride("font_size", 34);
        warningLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.92f, 0.15f));
        warningLabel.AddThemeColorOverride("font_outline_color", new Color(0.1f, 0.02f, 0.02f, 1.0f));
        warningLabel.AddThemeConstantOverride("outline_size", 5);
        AddChild(warningLabel);
        warningLabel.Hide();

        sprite.Visible = false;
        highlight.Visible = false;
    }

    public override void _Process(double delta)
    {
        elapsed += (float)delta;

        if (elapsed < WarningDuration)
        {
            popupScale = 0.0f;
            showWarning = true;

            GlobalPosition = edgeAnchor - poleDirection * poleReach;
            Scale = Vector2.One;

            sprite.Visible = false;
            highlight.Visible = false;
            warningLabel.Show();

            warningLabel.GlobalPosition = (edgeAnchor - poleDirection * (poleReach * 0.5f))
                                          - warningLabel.Size * 0.5f;
            QueueRedraw();
            return;
        }

        showWarning = false;
        warningLabel.Hide();
        sprite.Visible = true;
        highlight.Visible = true;
        QueueRedraw();

        float activeElapsed = elapsed - WarningDuration;
        float popInEnd = PopDuration;
        float holdEnd = popInEnd + HoldDuration;
        float popOutEnd = holdEnd + PopDuration;

        if (activeElapsed < popInEnd)
        {
            float t = Mathf.Clamp(activeElapsed / PopDuration, 0.0f, 1.0f);
            popupScale = Mathf.SmoothStep(0.0f, 1.0f, t);
        }
        else if (activeElapsed < holdEnd)
        {
            popupScale = 1.0f;
        }
        else if (activeElapsed < popOutEnd)
        {
            float t = Mathf.Clamp((activeElapsed - holdEnd) / PopDuration, 0.0f, 1.0f);
            popupScale = 1.0f - Mathf.SmoothStep(0.0f, 1.0f, t);
        }
        else
        {
            QueueFree();
            return;
        }

        GlobalPosition = edgeAnchor - poleDirection * (poleReach * popupScale);
        Scale = Vector2.One * popupScale;

        highlight.Scale = sprite.Scale;
        highlight.Offset = sprite.Offset;

        ZIndex = 0;
        Modulate = Colors.White;
        sprite.Modulate = Colors.White;
        highlightMaterial.SetShaderParameter("highlight_color", new Color(1.0f, 0.06f, 0.04f, 1.0f));

        bool shouldCollide = popupScale >= 0.45f;
        if (collisionActive != shouldCollide)
        {
            collisionShape.SetDeferred("disabled", !shouldCollide);
            poleCollision.SetDeferred("disabled", !shouldCollide);
            collisionActive = shouldCollide;
        }

        if (shouldCollide && !hasHitPlayer && airMeter?.IsForwardBoostActive() != true)
        {
            foreach (Node2D body in GetOverlappingBodies())
            {
                if (body is PlayerControl p)
                {
                    hasHitPlayer = true;
                    GetNode<SoundEffects>("/root/SoundEffects").PlaySignHit();
                    p.HitObstacle(GlobalPosition);
                    airMeter?.HitObstacle();
                    break;
                }
            }
        }
    }

    public override void _Draw()
    {
        if (!showWarning)
            return;

        float pulse = 0.85f + 0.15f * Mathf.Sin(elapsed * 10.0f);
        float warningWidth = Mathf.Max(36.0f, radius * 0.65f);
        Rect2 poleArea = new(-warningWidth * 0.5f, 0.0f, warningWidth, poleReach);

        DrawRect(poleArea, new Color(1.0f, 0.12f, 0.08f, 0.22f * pulse));
        DrawRect(poleArea, new Color(1.0f, 0.28f, 0.16f, 0.75f * pulse), false, 3.0f);
    }
}
