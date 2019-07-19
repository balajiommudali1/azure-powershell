﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Commands.Storage.Common
{
    using Microsoft.WindowsAzure.Storage;
    using System;
    using System.Threading;

    internal class CmdletOperationContext
    {
        private static volatile bool _inited;
        private static readonly object SyncRoot = new Object();

        public static DateTime StartTime
        {
            get;
            private set;
        }

        public static string ClientRequestId
        {
            get;
            private set;
        }

        /// <summary>
        /// started remote call counter
        /// </summary>
        private static int _startedRemoteCallCounter;

        public static int StartedRemoteCallCounter
        {
            get
            {
                return _startedRemoteCallCounter;
            }
        }

        /// <summary>
        /// finished remote call counter
        /// </summary>
        private static int _finishedRemoteCallCounter;

        public static int FinishedRemoteCallCounter
        {
            get
            {
                return _finishedRemoteCallCounter;
            }
        }

        private CmdletOperationContext() { }

        /// <summary>
        /// Init the cmdlet operation context
        /// </summary>
        public static void Init()
        {
            if (!_inited)
            {
                lock (SyncRoot)
                {
                    if (!_inited)
                    {
                        StartTime = DateTime.Now;
                        ClientRequestId = GenClientRequestID();
                        _inited = true;
                    }
                }
            }
        }

        /// <summary>
        /// Get an unique client request id
        /// </summary>
        /// <returns>A unique request id</returns>
        internal static string GenClientRequestID()
        {
            var uniqueId = System.Guid.NewGuid().ToString();
            return string.Format(ResourceV1.ClientRequestIdFormat, uniqueId);
        }

        /// <summary>
        /// Get Storage Operation Context for rest calls
        /// </summary>
        /// <param name="outputWriter">Output writer for writing logs for each rest call</param>
        /// <returns>Storage operation context</returns>
        public static OperationContext GetStorageOperationContext(Action<string> outputWriter)
        {
            if (!_inited)
            {
                Init();
            }

            var context = new OperationContext {ClientRequestID = ClientRequestId};

            context.SendingRequest += (s, e) =>
            {
                context.StartTime = DateTime.Now;

                Interlocked.Increment(ref _startedRemoteCallCounter);
// TODO: Remove IfDef
#if NETSTANDARD
                //https://github.com/Azure/azure-storage-net/issues/658
                var message = String.Format(ResourceV1.StartRemoteCall,
                    _startedRemoteCallCounter, String.Empty, e.RequestUri.ToString());
#else
                string message = String.Format(ResourceV1.StartRemoteCall,
                    _startedRemoteCallCounter, e.Request.Method, e.Request.RequestUri.ToString());
#endif

                try
                {
                    outputWriter?.Invoke(message);
                }
                catch
                {
                    //catch the exception. If so, the storage client won't sleep and retry
                }
            };

            context.ResponseReceived += (s, e) =>
            {
                context.EndTime = DateTime.Now;
                Interlocked.Increment(ref _finishedRemoteCallCounter);

                var elapsedTime = (context.EndTime - context.StartTime).TotalMilliseconds;
// TODO: Remove IfDef
#if NETSTANDARD
                //https://github.com/Azure/azure-storage-net/issues/658
                var message = String.Format(ResourceV1.FinishRemoteCall,
                    e.RequestUri.ToString(), String.Empty, String.Empty, e.RequestInformation.ServiceRequestID, elapsedTime);
#else
                string message = String.Format(ResourceV1.FinishRemoteCall,
                    e.Request.RequestUri.ToString(), (int)e.Response.StatusCode, e.Response.StatusCode, e.RequestInformation.ServiceRequestID, elapsedTime);
#endif

                try
                {
                    outputWriter?.Invoke(message);
                }
                catch
                {
                    //catch the exception. If so, the storage client won't sleep and retry
                }
            };

            return context;
        }

        /// <summary>
        /// Get the running ms from when operation context started
        /// </summary>
        /// <returns>A time string in ms</returns>
        public static double GetRunningMilliseconds()
        {
            if (!_inited)
            {
                return 0;
            }

            var span = DateTime.Now - StartTime;
            return span.TotalMilliseconds;
        }
    }
}
