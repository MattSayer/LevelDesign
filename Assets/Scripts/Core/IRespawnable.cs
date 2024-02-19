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
        BeforeRespawn
    }
}