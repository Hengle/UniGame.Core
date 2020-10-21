namespace UniModules.UniGame.Core.Runtime.Interfaces
{
    public interface IReadOnlyData
    {
        TData Get<TData>();
        bool Contains<TData>();
    }
}