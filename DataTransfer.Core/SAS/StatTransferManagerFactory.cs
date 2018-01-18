using System;
using System.Collections.Generic;

namespace DataTransfer.Core.SAS
{
    /// <summary>
    /// StatTransferManager 管理工厂
    /// </summary>
    public class StatTransferManagerFactory
    {
        private static Dictionary<string, StatTransferManager> TransferManagerPools = new Dictionary<string, StatTransferManager>();
        static StatTransferManagerFactory()
        {
            BuidPools();
        }
        private static void BuidPools()
        {
            TransferManagerPools.Add("LocalMachine", new StatTransferManager(false));
            TransferManagerPools.Add("LocalAssembly", new StatTransferManager(true));
        }
        public static StatTransferManager GetLocalMachineSTM()
        {
            return TransferManagerPools["LocalMachine"];
        }
        public static StatTransferManager GetAssemblySTM()
        {
            return TransferManagerPools["LocalAssembly"];
        }
    }
}
