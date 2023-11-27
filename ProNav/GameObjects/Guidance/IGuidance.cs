namespace ProNav.GameObjects.Guidance
{
    public interface IGuidance
    {
        D2DPoint ImpactPoint { get; set; }
        D2DPoint StableAimPoint { get; set; }
        D2DPoint CurrentAimPoint { get; set; }
        Missile Missile { get; set; }
        Target Target { get; set; }

        float GuideTo(float dt);
    }
}
