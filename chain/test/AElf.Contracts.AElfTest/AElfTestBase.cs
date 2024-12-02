
using System.Linq;
using AElf.Boilerplate.TestBase;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElfTest.Contract;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Contracts.AElfTest
{
    // ReSharper disable InconsistentNaming
    public class AElfTestBase : DAppContractTestBase<AElfTestModule>
    {
        
        internal AElfTestContractContainer.AElfTestContractStub AElfTestContractStub =>
            GetAElfTestContractStub(SampleAccount.Accounts.First().KeyPair);
        
        internal AElfTestContractContainer.AElfTestContractStub AElfTestContractSenderStub =>
            GetAElfTestContractStub(SampleAccount.Accounts[1].KeyPair);  

        
        internal AElfTestContractContainer.AElfTestContractStub  GetAElfTestContractStub(ECKeyPair senderKeyPair)
        {
            return Application.ServiceProvider.GetRequiredService<IContractTesterFactory>()
                .Create<AElfTestContractContainer.AElfTestContractStub>(DAppContractAddress,
                    senderKeyPair);
        }
    }
}