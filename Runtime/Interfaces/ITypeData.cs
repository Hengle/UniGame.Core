namespace UniModules.UniGame.Core.Runtime.Interfaces
{
    using System;
    using System.Collections.Generic;
    using UniCore.Runtime.ObjectPool.Runtime.Interfaces;
    using UniRx;

    public interface ITypeData : 
        IPoolable, 
        IMessageBroker,
        IValueContainerStatus, 
        IReadOnlyData
    {
        bool Remove<TData>();
        
        //Editor Only API
#if UNITY_EDITOR

        IReadOnlyDictionary<Type, IValueContainerStatus> EditorValues { get; }
        
#endif
    }
}