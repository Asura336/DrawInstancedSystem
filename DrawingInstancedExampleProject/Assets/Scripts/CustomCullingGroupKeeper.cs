using Com.Culling;

public class CustomCullingGroupKeeper : AABBCullingGroupKeeperTemplate<JobsAABBCullingGroup, JobsAABBCullingVolume>
{
    static CustomCullingGroupKeeper instance;
    public static CustomCullingGroupKeeper Instance => instance;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        instance = this;
    }
}
