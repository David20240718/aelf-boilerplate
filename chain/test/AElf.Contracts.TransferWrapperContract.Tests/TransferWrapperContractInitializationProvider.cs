using System.Collections.Generic;
using AElf.Boilerplate.TestBase;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Contracts.TransferWrapperContract
{
    public class TransferWrapperContractInitializationProvider : IContractInitializationProvider
    {
        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>();
        }

        public Hash SystemSmartContractName { get; } = DAppSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.TransferWrapperContract";
    }
}