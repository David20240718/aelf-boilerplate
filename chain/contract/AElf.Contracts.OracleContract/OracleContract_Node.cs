using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.OracleContract
{
    public partial class OracleContract
    {
        public override Empty DepositEscrow(DepositEscrowInput input)
        {
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                Symbol = TokenSymbol,
                From = Context.Sender,
                To = Context.Self,
                Amount = input.Amount
            });
            var nodeInfo = State.NodeInfo[Context.Sender] ?? new NodeInfo();
            nodeInfo.Escrow = nodeInfo.Escrow.Add(input.Amount);
            State.NodeInfo[Context.Sender] = nodeInfo;
            return new Empty();
        }

        public override Empty WithdrawEscrow(WithdrawEscrowInput input)
        {
            Assert(!State.QuestionableNodes[Context.Sender], "Questionable node");
            Assert(!State.AuthorizedNodes[Context.Sender], "Should be remove form node list firstly");
            var nodeInfo = State.NodeInfo[Context.Sender];
            Assert(nodeInfo != null && nodeInfo.Escrow >= input.Amount, "Invalid amount");
            nodeInfo.Escrow = nodeInfo.Escrow.Sub(input.Amount);
            State.TokenContract.Transfer.Send(new TransferInput
            {
                Symbol = TokenSymbol,
                To = Context.Self,
                Amount = input.Amount
            });
            if (nodeInfo.Escrow == 0 && nodeInfo.Withdrawable == 0)
            {
                State.NodeInfo.Remove(Context.Sender);
            }
            else
            {
                State.NodeInfo[Context.Sender] = nodeInfo;
            }

            return new Empty();
        }

        public override Empty WithdrawRevenue(Empty input)
        {
            Assert(!State.QuestionableNodes[Context.Sender], "Questionable node");
            var nodeInfo = State.NodeInfo[Context.Sender];
            if (nodeInfo == null)
            {
                return new Empty();
            }

            var withdrawAmount = nodeInfo.Withdrawable;
            if (withdrawAmount == 0)
            {
                return new Empty();
            }
            nodeInfo.Withdrawable = 0;
            State.NodeInfo[Context.Sender] = nodeInfo;
            State.TokenContract.Transfer.Send(new TransferInput
            {
                Symbol = TokenSymbol,
                To = Context.Sender,
                Amount = withdrawAmount
            });
            return new Empty();
        }


        public override Empty AddNode(AddNodeInput input)
        {
            Assert(Context.Sender == State.Controller.Value, "Not authorized");
            if (State.AuthorizedNodes[input.Node])
            {
                return new Empty();
            }

            var nodeInfo = State.NodeInfo[Context.Sender];
            if (input.Amount > 0)
            {
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    Symbol = TokenSymbol,
                    From = input.Node,
                    To = Context.Self,
                    Amount = input.Amount
                });
                nodeInfo.Escrow = nodeInfo.Escrow.Add(input.Amount);
            }

            Assert(nodeInfo != null && nodeInfo.Escrow >= State.MinimumEscrow.Value, "Insufficient escrow");
            State.AuthorizedNodes[Context.Sender] = true;
            var nodeList = State.AvailableNodes.Value;
            nodeList.NodeList.Add(input.Node);
            State.AvailableNodes.Value = nodeList;
            return new Empty();
        }

        public override Empty AddQuestionableNode(Address input)
        {
            Assert(Context.Sender == State.Controller.Value, "Not authorized");
            State.QuestionableNodes[input] = true;
            return new Empty();
        }
        
        public override Empty RemoveQuestionableNode(Address input)
        {
            Assert(Context.Sender == State.Controller.Value, "Not authorized");
            State.QuestionableNodes.Remove(input);
            return new Empty();
        }

        public override Empty RemoveNode(Address input)
        {
            Assert(Context.Sender == State.Controller.Value, "Not authorized");
            State.AuthorizedNodes.Remove(Context.Sender);
            var nodeList = State.AvailableNodes.Value;
            nodeList.NodeList.Remove(input);
            State.AvailableNodes.Value = nodeList;
            return new Empty();
        }

        public override Empty RemoveNodeStatistic(Address input)
        {
            Assert(Context.Sender == State.Controller.Value, "Not authorized");
            State.NodeStatistic.Remove(input);
            return new Empty();
        }
    }
}