using Godot;

public sealed class SpawnPosition
{
    private enum HazardEdge
    {
        Bottom,
        Top,
        Left,
        Right
    }

    public bool TryFindPosition(Node2D spawnable, Rect2 viewport, Node2D spawnablesContainer,
        float minimumSeparation, out Vector2 spawnPosition)
    {
        Vector2 viewportCenter = viewport.Position + viewport.Size / 2f;
        HazardEdge? hazardEdge = null;

        if (spawnable is FlyingHazard hazard)
        {
            Vector2 playPoint = viewportCenter + new Vector2(
                (float)GD.RandRange(-viewport.Size.X * 0.45f, viewport.Size.X * 0.45f),
                (float)GD.RandRange(-viewport.Size.Y * 0.40f, viewport.Size.Y * 0.40f));

            float distanceLeft = playPoint.X - viewport.Position.X;
            float distanceRight = viewport.End.X - playPoint.X;
            float distanceTop = playPoint.Y - viewport.Position.Y;
            float distanceBottom = viewport.End.Y - playPoint.Y;
            float minimumEdgeDistance = Mathf.Min(Mathf.Min(distanceLeft, distanceRight),
                Mathf.Min(distanceTop, distanceBottom));

            if (Mathf.IsEqualApprox(minimumEdgeDistance, distanceLeft)) hazardEdge = HazardEdge.Left;
            else if (Mathf.IsEqualApprox(minimumEdgeDistance, distanceRight)) hazardEdge = HazardEdge.Right;
            else if (Mathf.IsEqualApprox(minimumEdgeDistance, distanceTop)) hazardEdge = HazardEdge.Top;
            else hazardEdge = HazardEdge.Bottom;

            Vector2 poleDirection = hazardEdge.Value switch
            {
                HazardEdge.Left => Vector2.Left,
                HazardEdge.Right => Vector2.Right,
                HazardEdge.Top => Vector2.Up,
                _ => Vector2.Down
            };
            hazard.ConfigurePoleDirection(poleDirection);
        }

        for (int attempt = 0; attempt < 24; attempt++)
        {
            Vector2 candidate = GetCandidatePosition(spawnable, hazardEdge, viewport, viewportCenter);
            if (IsPositionClear(candidate, spawnablesContainer, minimumSeparation))
            {
                spawnPosition = candidate;
                return true;
            }
        }

        spawnPosition = Vector2.Zero;
        return false;
    }

    private static Vector2 GetCandidatePosition(Node2D spawnable, HazardEdge? hazardEdge,
        Rect2 viewport, Vector2 viewportCenter)
    {
        switch (hazardEdge)
        {
            case HazardEdge.Left:
                return new Vector2(viewport.Position.X - 8f,
                    (float)GD.RandRange(viewport.Position.Y + viewport.Size.Y * 0.2f,
                        viewport.End.Y - viewport.Size.Y * 0.2f));
            case HazardEdge.Right:
                return new Vector2(viewport.End.X + 8f,
                    (float)GD.RandRange(viewport.Position.Y + viewport.Size.Y * 0.2f,
                        viewport.End.Y - viewport.Size.Y * 0.2f));
            case HazardEdge.Top:
                return new Vector2(
                    (float)GD.RandRange(viewport.Position.X + viewport.Size.X * 0.15f,
                        viewport.End.X - viewport.Size.X * 0.15f), viewport.Position.Y - 8f);
            case HazardEdge.Bottom:
                return new Vector2(
                    (float)GD.RandRange(viewport.Position.X + viewport.Size.X * 0.15f,
                        viewport.End.X - viewport.Size.X * 0.15f), viewport.End.Y + 8f);
            default:
                if (spawnable is AirRing)
                    return new Vector2(
                        (float)GD.RandRange(viewport.Position.X + viewport.Size.X * 0.10f,
                            viewport.End.X - viewport.Size.X * 0.10f),
                        (float)GD.RandRange(viewport.Position.Y + viewport.Size.Y * 0.15f,
                            viewport.End.Y - viewport.Size.Y * 0.15f));

                return viewportCenter + new Vector2(
                    (float)GD.RandRange(-viewport.Size.X * 0.32f, viewport.Size.X * 0.32f),
                    (float)GD.RandRange(-viewport.Size.Y * 0.24f, viewport.Size.Y * 0.24f));
        }
    }

    private static bool IsPositionClear(Vector2 candidate, Node2D spawnablesContainer, float minimumSeparation)
    {
        foreach (Node child in spawnablesContainer.GetChildren())
        {
            if (child is Node2D existing &&
                existing.GlobalPosition.DistanceTo(candidate) < minimumSeparation)
                return false;
        }

        return true;
    }
}
