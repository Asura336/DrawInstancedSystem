using Com.Culling;

public class CustomAABBCullingVolume : AABBCullingVolumeTemplate<CustomCullingGroupKeeper>
{
    protected override CustomCullingGroupKeeper FindGroupKeeper()
    {
        return CustomCullingGroupKeeper.Instance;
    }
}
