﻿namespace UniModules.UniGame.Context.SerializableContext.Runtime.States
{
    using System;
    using Core.Runtime.AsyncOperations;
    using Core.Runtime.DataFlow.Interfaces;
    using Core.Runtime.Interfaces;
    using Cysharp.Threading.Tasks;

    [Serializable]
    public class AsyncStateProxyValue<TData, TValue> : 
        AsyncState<TData,TValue>,
        IAsyncRollback<TData>
    {
        private readonly IAsyncCommand<TData, TValue>    _command;
        private readonly IAsyncCompletion<TValue, TData> _onComplete;
        private readonly IAsyncEndPoint<TData>           _endPoint;
        private readonly IAsyncRollback<TData>           _onRollback;

        public AsyncStateProxyValue(
            IAsyncCommand<TData,TValue> command = null,
            IAsyncCompletion<TValue,TData> onComplete = null,
            IAsyncEndPoint<TData> endPoint = null,
            IAsyncRollback<TData> onRollback = null)
        {
            _command    = command;
            _onComplete = onComplete;
            _endPoint   = endPoint;
            _onRollback = onRollback;
        }

        public async UniTask Rollback(TData source)
        {
            if (_onRollback != null)
                await _onRollback.Rollback(source);
        }
        
        
        protected override async UniTask OnComplete(TValue value, TData context, ILifeTime lifeTime) {
            if (_onComplete == null)
                return;
            await _onComplete.Complete(value, context, lifeTime);
        }
        
        protected override async UniTask<TValue> OnExecute(TData context, ILifeTime lifeTime) {
            if (_command == null)
                return default;
            return await _command.Execute(context).
                WithCancellation(lifeTime.AsCancellationToken());
        }

        protected override async UniTask OnExit(TData data) {
            if (_endPoint == null)
                return;
            await _endPoint.Exit(data);
        }
    }
}