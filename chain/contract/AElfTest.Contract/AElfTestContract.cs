using System;
using System.Text;
using System.Text.RegularExpressions;
using AElf;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElfTest.Contract
{
    /// <summary>
    /// The C# implementation of the contract defined in hello_world_contract.proto that is located in the "protobuf"
    /// folder.
    /// Notice that it inherits from the protobuf generated code. 
    /// </summary>
    public partial class AElfTestContract : AElfTestContractContainer.AElfTestContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.Owner.Value == null, "Already initialized.");
            State.Owner.Value = input.Owner;
            if (State.TokenContract.Value == null)
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            if (State.ConfigurationContract.Value == null)
                State.ConfigurationContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ConfigurationContractSystemName);
            return new Empty();
        }
        
        public override Empty TestCreate(TestCreateInput input)
        {
            Assert( State.Owner.Value == Context.Sender, "No permission.");
            Assert(!string.IsNullOrWhiteSpace(input.Symbol), "Invalid symbol.");
            Assert(input.TotalSupply > 0, "Invalid total supply.");
            Assert(State.TokenInfoMap[input.Symbol] == null, $"Token {input.Symbol} already exists.");

            var tokenInfo = new TestTokenInfo
            {
                Symbol = input.Symbol,
                TotalSupply = input.TotalSupply
            };
            State.TokenInfoMap[input.Symbol] = tokenInfo;
            State.BalanceMap[Context.Sender][input.Symbol] = input.TotalSupply;

            Context.Fire(new TestTokenCreated
            {
                Symbol = tokenInfo.Symbol,
                TotalSupply = tokenInfo.TotalSupply,
            });
            return new Empty();
        }

        /// <summary>
        /// Removes the token information associated with the given symbol from the state.
        /// </summary>
        /// <param name="input">
        /// A <see cref="StringValue"/> object containing the symbol of the token to be removed.
        /// </param>
        /// <returns>
        /// An <see cref="Empty"/> object indicating the operation has completed.
        /// </returns>
        /// <remarks>
        /// This method first validates that the token with the specified symbol exists using the <see cref="ValidTokenExisting"/> method.
        /// If the token exists, it is removed from the <see cref="State.TokenInfoMap"/>. 
        /// After the removal, the <see cref="TestTokenRemoved"/> event is fired, which includes the symbol of the removed token.
        /// </remarks>
        public override Empty TestRemove(StringValue input)
        {
            var tokenInfo = ValidTokenExisting(input.Value);
            State.TokenInfoMap.Remove(input.Value);
            Context.Fire(new TestTokenRemoved
            {
                Symbol = tokenInfo.Symbol,
            });
            return new Empty();
        }

        public override Empty TestTransfer(TestTransferInput input)
        {
            Assert( input.Amount > 0);
            ValidTokenExisting(input.Symbol);
            DoTransfer(Context.Sender, input.To, input.Symbol, input.Amount, input.Memo);
            
            return new Empty();
        }
        
        public override Empty TransferWithoutParallel(TestTransferInput input)
        {
            Assert( input.Amount > 0, "Invalid amount.");
            ValidTokenExisting(input.Symbol);
            DoTransfer(Context.Sender, input.To, input.Symbol, input.Amount, input.Memo);
           
            return new Empty();
        }

        /// <summary>
        /// Adds an account to the AccountList state.
        /// If the AccountList does not exist, it initializes a new list.
        /// Fires an AccountAdded event after successfully adding the account.
        /// </summary>
        /// <param name="input">The address of the account to be added.</param>
        /// <returns>An Empty object indicating the operation is complete.</returns>
        public override Empty AddAccount(Address input)
        {
//            var accountList = State.AccountList?.Value ?? new AccountList();
            var accountList = State.AccountList?.Value;
            accountList.Account.Add(input);
            State.AccountList.Value = accountList;
            Context.Fire(new AccountAdded
            {
                Account = input
            });
            return new Empty();
        }
        
        public override Empty RemoveAccount(Address input)
        {
            var accountList = State.AccountList.Value;
            if (accountList == null || !accountList.Account.Contains(input))
            {
                return new Empty();
            }
            accountList.Account.Remove(input);
            State.AccountList.Value = accountList;
            Context.Fire(new AccountRemoved()
            {
                Account = input
            });
            return new Empty();
        }
        //
        // public override Empty SetLongKeyValue(LongKeyInput input)
        // {
        //     var randomCheck = new Random();
        //     var key = Regex.IsMatch(input.Key, "^[a-zA-Z0-9]+$");
        //     Assert(key, "Invalid key.");
        //     State.LongKeyMap[input.Key] = input.Length;
        //     
        //     return new Empty();
        // }

        public override Int64Value GetLongKey(StringValue input)
        {
            return new Int64Value
            {
                Value = State.LongKeyMap[input.Value] 
            };
        }


        public override TestBalance GetTestBalance(GetTestBalanceInput input)
        {
            var owner = input.Owner ?? Context.Sender;
            return new TestBalance
            {
                Amount = State.BalanceMap[owner][input.Symbol],
                Owner = owner,
                Symbol = input.Symbol
            };
        }
        
        public override TestTokenInfo GetTestTokenInfo(GetTestTokenInfoInput input)
        {
            return State.TokenInfoMap[input.Symbol];
        }

        public override AccountList GetAccountList(Empty input)
        {
            var accountList = State.AccountList.Value;
            return accountList ?? new AccountList();
        }
        
        public long GetBalance(GetTestBalanceInput input)
        {
            var owner = input.Owner ?? Context.Sender;
            return State.BalanceMap[owner][input.Symbol];
        }


        private void ModifyBalance(Address address, string symbol, long addAmount)
        {
            var before = GetBalance(address, symbol);
            if (addAmount < 0 && before < -addAmount)
            {
                Assert(false,
                    $"Insuffici balance of {symbol}. Need balance: {-addAmount}; Current balance: {before}");
            }

            var target = before.Add(addAmount);
            State.BalanceMap[address][symbol] = target;
        }
        
        private TestTokenInfo ValidTokenExisting(string symbol)
        {
            var tokenInfo = State.TokenInfoMap[symbol];
            if (tokenInfo == null)
            {
                throw new AssertionException($"Token {symbol} not found.");
            }

            return tokenInfo;
        }
        
        private void DoTransfer(Address from, Address to, string symbol, long amount, ByteString memo = null)
        {
            Assert(from != to, "Can't do transfer to sender itself.");
            ModifyBalance(from, symbol, -amount);
            ModifyBalance(to, symbol, amount);
            Context.Fire(new TestTransferred
            {
                From = from,
                To = to,
                Symbol = symbol,
                Amount = amount,
                Memo = memo ?? ByteString.Empty
            });
        }
        
        public override Empty Transfer(TransferInput input)
        {
            var tokenInfo = AssertValidToken(input.Symbol, input.Amount);
            DoTransfer(Context.Sender, input.To, tokenInfo.Symbol, input.Amount, input.Memo);
            DealWithExternalInfoDuringTransfer(new TransferFromInput
            {
                From = Context.Sender,
                To = input.To,
                Amount = input.Amount,
                Symbol = tokenInfo.Symbol,
                Memo = input.Memo
            });
            return new Empty();
        }
        
        
        
        public override Empty TransferFrom(TransferFromInput input)
        {
            var tokenInfo = AssertValidToken(input.Symbol, input.Amount);
            DoTransferFrom(input.From, input.To, Context.Sender, tokenInfo.Symbol, input.Amount, input.Memo);
            return new Empty();
        }
        
    
        
        private TokenInfo GetTokenInfo(string symbolOrAlias)
        {
            var tokenInfo = State.TokenInfos[symbolOrAlias];
            if (tokenInfo != null) return tokenInfo;
            var actualTokenSymbol = State.SymbolAliasMap[symbolOrAlias];
            if (!string.IsNullOrEmpty(actualTokenSymbol))
            {
                tokenInfo = State.TokenInfos[actualTokenSymbol];
            }

            return tokenInfo;
        }
        
        private void AssertValidSymbolAndAmount(string symbol, long amount)
        {
            Assert(!string.IsNullOrEmpty(symbol) && IsValidSymbol(symbol),
                "Invalid symbol.");
            Assert(amount > 0, "Invalid amount.");
        }
        
        private static bool IsValidSymbol(string symbol)
        {
            return Regex.IsMatch(symbol, "^[a-zA-Z0-9]+(-[0-9]+)?$");
        }

       
        
        private void DealWithExternalInfoDuringTransfer(TransferFromInput input)
        {
            var tokenInfo = GetTokenInfo(input.Symbol);
            if (tokenInfo.ExternalInfo == null) return;
            if (tokenInfo.ExternalInfo.Value.ContainsKey(AElfTestTokenContractConstants.TransferCallbackExternalInfoKey))
            {
                var callbackInfo =
                    JsonParser.Default.Parse<CallbackInfo>(
                        tokenInfo.ExternalInfo.Value[AElfTestTokenContractConstants.TransferCallbackExternalInfoKey]);
                Context.SendInline(callbackInfo.ContractAddress, callbackInfo.MethodName, input);
            }

            FireExternalLogEvent(tokenInfo, input);
        }
        


        private long GetBalance(Address address, string symbol)
        {
            return State.BalanceMap[address][symbol];
        }
    }

}