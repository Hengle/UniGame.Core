﻿namespace UniModules.UniCore.Runtime.Interfaces.Rx
{
    using System;

    public interface IObservableFactory<T>
    {

        IObservable<T> Create();

    }
}
