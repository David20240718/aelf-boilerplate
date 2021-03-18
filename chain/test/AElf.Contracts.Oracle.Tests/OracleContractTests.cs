using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Standards.ACS13;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContractTests : OracleContractTestBase
    {
        private async Task InitializeOracleContractAsync()
        {
            await OracleContractStub.Initialize.SendAsync(new InitializeInput
            {
                DefaultMinimumAvailableNodesCount = DefaultMinimumAvailableNodesCount,
                DefaultThresholdResponses = DefaultThresholdResponses,
                DefaultThresholdToUpdateData = DefaultThresholdToUpdateData,
                MinimumEscrow = DefaultMinimumEscrow,
                ClearRedundantRevenue = DefaultClearRedundantRevenue,
                ExpirationSeconds = DefaultExpirationSeconds
            });
        }

        private async Task TransferTokenOwnerAsync()
        {
            var defaultParliament = await GetDefaultParliament();
            await DefaultParliamentProposeAndRelease(new CreateProposalInput
            {
                ToAddress = TokenContractAddress,
                ContractMethodName = nameof(TokenContractContainer.TokenContractStub.ChangeTokenIssuer),
                OrganizationAddress = defaultParliament,
                Params = new ChangeTokenIssuerInput
                {
                    NewTokenIssuer = DefaultSender,
                    Symbol = TokenSymbol
                }.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            });
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = TokenSymbol,
                To = DefaultSender,
                Amount = 1000_00000000
            });
            await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Amount = 1000_00000000,
                Symbol = TokenSymbol,
                Spender = DAppContractAddress
            });
        }

        private async Task<IList<OracleContractContainer.OracleContractStub>> CreateOracleNode(int count)
        {
            var nodeAccounts = GetNodes(count).ToList();
            OracleNodes.Clear();
            foreach (var node in nodeAccounts)
            {
                await TokenContractStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = TokenSymbol,
                    Amount = DefaultMinimumEscrow,
                    To = node.Address
                });
                var nodeOracleStub = GetOracleContractStub(node.KeyPair);
                var tokenStub = GetTokenContractStub(node.KeyPair);
                await tokenStub.Approve.SendAsync(new ApproveInput
                {
                    Amount = DefaultMinimumEscrow,
                    Symbol = TokenSymbol,
                    Spender = DAppContractAddress
                });
                await nodeOracleStub.DepositEscrow.SendAsync(new DepositEscrowInput
                {
                    Amount = DefaultMinimumEscrow
                });
                await OracleContractStub.AddNode.SendAsync(new AddNodeInput
                {
                    Node = node.Address
                });
                OracleNodes.Add(nodeOracleStub);
            }

            return OracleNodes;
        }

        [Fact]
        private async Task<RequestCreated> CreateRequest_Success_Test()
        {
            await InitializeOracleContractAsync();
            await TransferTokenOwnerAsync();
            await CreateOracleNode(DefaultMinimumAvailableNodesCount);
            var ret = await OracleContractStub.CreateRequest.SendAsync(new CreateRequestInput
            {
                UrlToQuery = "wwww.adc.com",
                AttributeToFetch = "id",
                Payment = 100,
                MethodName = "",
                CallbackAddress = new Address()
            });
            var newRequest = new RequestCreated();
            newRequest.MergeFrom(ret.TransactionResult.Logs.First(l => l.Name == nameof(RequestCreated)));
            newRequest.RoundId.ShouldBe(1);
            return newRequest;
        }

        [Fact]
        private async Task<RequestCreated> SendHashData_Success_Test()
        {
            var requestCreated = await CreateRequest_Success_Test();
            var realValue = new Int32Value
            {
                Value = 32
            };
            var byteStringValue = realValue.ToByteString();
            var salt = "SALT";
            var dataHash = ComputeDataHash(byteStringValue, salt);
            var count = 0;
            foreach (var oracleNodeStub in OracleNodes)
            {
                var ret = await oracleNodeStub.SendHashData.SendAsync(new SendHashDataInput
                {
                    Payment = requestCreated.Payment,
                    CallbackAddress = requestCreated.CallbackAddress,
                    CancelExpiration = requestCreated.CancelExpiration,
                    RequestId = requestCreated.RequestId,
                    HashData = dataHash,
                    MethodName = requestCreated.MethodName
                });
                count++;
                if (count != DefaultThresholdResponses) continue;
                var getEnoughData = new SufficientDataCollected();
                getEnoughData.MergeFrom(ret.TransactionResult.Logs.First(x => x.Name == nameof(SufficientDataCollected)));
                getEnoughData.RequestId.ShouldBe(requestCreated.RequestId);
                break;
            }

            return requestCreated;
        }

        [Fact]
        public async Task SendDataWithSalt_Success_Test()
        {
            var newRequest = await SendHashData_Success_Test();
            var realValue = new Int32Value
            {
                Value = 32
            };
            var byteStringValue = realValue.ToByteString();
            var salt = "SALT";
            var count = 0;
            foreach (var oracleNodeStub in OracleNodes)
            {
                var ret = await oracleNodeStub.SendDataWithSalt.SendAsync(new SendDataWithSaltInput
                {
                    Payment = newRequest.Payment,
                    CallbackAddress = newRequest.CallbackAddress,
                    CancelExpiration = newRequest.CancelExpiration,
                    RequestId = newRequest.RequestId,
                    MethodName = newRequest.MethodName,
                    RealData = byteStringValue,
                    Salt = salt
                });
                count++;
                if (count != DefaultThresholdResponses) continue;
                var answerUpdated = new AnswerUpdated();
                answerUpdated.MergeFrom(ret.TransactionResult.Logs.First(x => x.Name == nameof(AnswerUpdated)));
                answerUpdated.RequestId.ShouldBe(newRequest.RequestId);
                var lastValue = Int32Value.Parser.ParseFrom(answerUpdated.AgreedValue);
                lastValue.Value.ShouldBe(32);
                break;
            }
            
        }

        private Hash ComputeDataHash(ByteString rawData, string salt)
        {
            var saltHash = HashHelper.ComputeFrom(salt);
            var dataHash = HashHelper.ComputeFrom(rawData.ToByteArray());
            return HashHelper.ConcatAndCompute(dataHash, saltHash);
        }
    }
}