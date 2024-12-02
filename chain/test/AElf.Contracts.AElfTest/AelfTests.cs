using System.Linq;
using System.Threading.Tasks;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using AElfTest.Contract;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AElfTest
{
    // ReSharper disable InconsistentNaming
    public class AelfTests : AElfTestBase
    {
        private const long Price = 10_00000000;

        [Fact]
        public async Task TestInitialize()
        {
            Account owner = SampleAccount.Accounts.First();

            await AElfTestContractStub.Initialize.SendAsync(new InitializeInput
            {
                Owner = owner.Address,
            });
        }

        [Fact]
        public async Task TestCreateToken()
        {
            Account owner = SampleAccount.Accounts.First();

            await AElfTestContractStub.Initialize.SendAsync(new InitializeInput
            {
                Owner = owner.Address,
            });


            await AElfTestContractStub.TestCreate.SendAsync(
                new TestCreateInput
                {
                    Symbol = "ELF_test",
                    TotalSupply = 100000000000
                });
        }

        [Fact]
        public async Task TestTransferFrom()
        {
            Account owner = SampleAccount.Accounts.First();

            await AElfTestContractStub.Initialize.SendAsync(new InitializeInput
            {
                Owner = owner.Address,
            });


            await AElfTestContractStub.TestCreate.SendAsync(
                new TestCreateInput
                {
                    Symbol = "ELFtest",
                    TotalSupply = 100000000000
                }
            );

            await AElfTestContractStub.Transfer.SendAsync(
                new TransferInput()
                {
                    To = SampleAccount.Accounts[1].Address,
                    Symbol = "ELFtest",
                    Amount = 456789,
                    Memo = "ELFtest"
                }
            );
            
            
            // await AElfTestContractSenderStub.Approve.SendAsync(
            //     new ApproveInput
            //     {
            //         Spender = SampleAccount.Accounts[1].Address,
            //         Symbol = "ELFtest",
            //         Amount = 100000
            //     }
            // );
            
            var result2 = await AElfTestContractSenderStub.TransferFrom.SendAsync(
                new TransferFromInput()
                {
                    From = SampleAccount.Accounts[1].Address,
                    To = SampleAccount.Accounts[3].Address,
                    Symbol = "ELFtest",
                    Amount = 56789,
                    Memo = "ELFtest"
                }
            );
            
            result2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}