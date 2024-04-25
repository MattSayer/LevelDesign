namespace AmalgamGames.Core
{
    public interface IRespawnable
    {
        public void OnRespawnEvent(RespawnEvent evt);
    }

    public enum RespawnEvent
    {
        OnCollision,
        OnRespawnStart,
        OnRespawnEnd,
        BeforeRespawn,
        OnCheckpoint,
        OnInitialSpawnEnded
    }

    public class RespawnEventInfo
    {
        public RespawnEvent Event;

        public RespawnEventInfo(RespawnEvent @event)
        {
            Event = @event;
        }
    }
}