namespace AmalgamGames.Core
{
    public interface IValueProvider
    {
        public void SubscribeToValue(string valueName, System.Action<object> callback);
        public void UnsubscribeFromValue(string valueName, System.Action<object> callback);
    }
}