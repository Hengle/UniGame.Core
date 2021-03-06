﻿namespace UniModules.UniGame.Core.Runtime.AsyncOperations
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DataFlow.Interfaces;
    using Interfaces;
    using UnityEngine;

    [Serializable]
    public class AsyncScenario<TCommand,TData> : AsyncState<TData>
        where TCommand : IAsyncCommand<TData,AsyncStatus>
    {
        #region inspector
        
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.ListDrawerSettings(Expanded = true)]
#endif
        [SerializeReference]
        public List<TCommand> commands = new List<TCommand>();

        [SerializeField] 
        private int _activeScenarioIndex = 0;
        
        #endregion

        #region constructor

        public AsyncScenario() { }
        
        public AsyncScenario(IEnumerable<TCommand> nodes) {
            commands.AddRange(nodes);
        }
        
        #endregion
        
        public async UniTask Rollback(IContext source) {
            
            for (var i = _activeScenarioIndex; i >=0 ; i--) 
            {
                switch (commands[i]) {
                    case  IAsyncRollback<IContext> contextRollback:
                        await contextRollback.Rollback(source);
                        break;
                    case  IAsyncRollback rollback:
                        await rollback.Rollback();
                        break;
                }
            }
        }

        protected override async UniTask OnExit(TData data)
        {

            for (int i = commands.Count; i >= 0; i++)
            {
                var scenario = commands[i];
                switch (scenario)
                {
                    case IAsyncEndPoint<TData> dataEndPoint:
                        await dataEndPoint.Exit(data);
                        break;
                    case IAsyncEndPoint endPoint:
                        await endPoint.Exit();
                        break;
                }
            }
            
        }

        protected sealed override async UniTask<AsyncStatus> OnExecute(TData context, ILifeTime executionLifeTime) {

            var asCancellationSource = executionLifeTime.AsCancellationSource();
            var result               = AsyncStatus.Pending;
            _activeScenarioIndex = 0;
            
            for (var i = 0; i < commands.Count; i++) {
                var asyncScenario = commands[i];
                var task          = asyncScenario.Execute(context).
                    WithCancellation(asCancellationSource.Token);
                
                result = await task;
                    
                if (result == AsyncStatus.Succeeded) {
                    continue;
                }

                _activeScenarioIndex = i;
                break;
            }

            return result;
        }

    }
}