using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Standards.ACS12;
using AElf.Standards.ACS2;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElfTest.Contract;

public partial class AElfTestContract
{
     public override ResourceInfo GetResourceInfo(Transaction txn)
        {
            switch (txn.MethodName)
            {
                case nameof(TestTransfer):
                {
                    var args = TestTransferInput.Parser.ParseFrom(txn.Params);
                    var resourceInfo = new ResourceInfo
                    {
                        WritePaths =
                        {
                            GetPath(nameof(AElfTestContractState.BalanceMap), txn.From.ToString(), args.Symbol),
                            GetPath(nameof(AElfTestContractState.BalanceMap), args.To.ToString(), args.Symbol),
                        },
                        ReadPaths =
                        {
                            GetPath(nameof(AElfTestContractState.TokenInfoMap), args.Symbol)
                        }
                    };
                    AddPathForTransactionFeeFreeAllowance(resourceInfo, txn.From);
                    AddPathForTransactionFee(resourceInfo, txn.From.ToString(), txn.MethodName);
                    AddPathForDelegatees(resourceInfo, txn.From, txn.To, txn.MethodName);

                    return resourceInfo;
                }
                
                default:
                    return new ResourceInfo {NonParallelizable = true};
            }
        }

     private ScopedStatePath GetPath(params string[] parts)
     {
         return new ScopedStatePath
         {
             Address = Context.Self,
             Path = new StatePath
             {
                 Parts =
                 {
                     parts
                 }
             }
         };
     }
     private ScopedStatePath GetPath(Address address, params string[] parts)
     {
         return new ScopedStatePath
         {
             Address = address,
             Path = new StatePath
             {
                 Parts =
                 {
                     parts
                 }
             }
         };
     }
     
     private void AddPathForTransactionFeeFreeAllowance(ResourceInfo resourceInfo, Address from)
     {
         var getTransactionFeeFreeAllowancesConfigOutput =
             State.TokenContract.GetTransactionFeeFreeAllowancesConfig.Call(new Empty());
         if (getTransactionFeeFreeAllowancesConfigOutput != null)
         {
             foreach (var symbol in getTransactionFeeFreeAllowancesConfigOutput.Value.Select(config => config.Symbol))
             {
                 resourceInfo.WritePaths.Add(GetPath(State.TokenContract.Value, "TransactionFeeFreeAllowances",
                     from.ToString(), symbol));
                 resourceInfo.WritePaths.Add(GetPath(State.TokenContract.Value,
                     "TransactionFeeFreeAllowancesLastRefreshTimes", from.ToString(), symbol));

                 var path = GetPath(State.TokenContract.Value, "TransactionFeeFreeAllowancesConfigMap", symbol);
                 if (!resourceInfo.ReadPaths.Contains(path))
                 {
                     resourceInfo.ReadPaths.Add(path);
                 }
             }
         }
     }
     
     
     private void AddPathForTransactionFee(ResourceInfo resourceInfo, string from, string methodName)
     {
         var symbols = GetTransactionFeeSymbols(methodName);
         var primaryTokenSymbol = State.TokenContract.GetPrimaryTokenSymbol.Call(new Empty()).Value;
         if (!symbols.Contains(primaryTokenSymbol))
             symbols.Add(primaryTokenSymbol);
         var paths = symbols.Select(symbol => GetPath(State.TokenContract.Value, "Balances", from, symbol));
         foreach (var path in paths)
         {
             if (resourceInfo.WritePaths.Contains(path)) continue;
             resourceInfo.WritePaths.Add(path);
         }
     }
     
     private void AddPathForDelegatees(ResourceInfo resourceInfo, Address from, Address to, string methodName)
     {
         var delegateeList = new List<Address>();
         //get and add first-level delegatee list
         delegateeList.AddRange(GetDelegateeList(from, to, methodName));
         if (delegateeList.Count <= 0) return;
         var secondDelegateeList = new List<Address>();
         //get and add second-level delegatee list
         foreach (var delegateeAddress in delegateeList)
         {
             //delegatee of the first-level delegate is delegator of the second-level delegate
             secondDelegateeList.AddRange(GetDelegateeList(delegateeAddress, to, methodName));
         }

         delegateeList.AddRange(secondDelegateeList);
         foreach (var delegatee in delegateeList.Distinct())
         {
             AddPathForTransactionFee(resourceInfo, delegatee.ToString(), methodName);
             AddPathForTransactionFeeFreeAllowance(resourceInfo, delegatee);
         }
     }
     private SymbolListToPayTxSizeFee GetSizeFeeSymbols()
     {
          var symbolListToPayTxSizeFee = State.TokenContract.GetSymbolsToPayTxSizeFee.Call(new Empty());
          return symbolListToPayTxSizeFee;
     }
     
     

     private List<string> GetMethodsFeeSymbols(string methodName)
     {
         var symbols = new List<string>();
         var methodFees = State.TokenContract.GetMethodFee.Call(new StringValue{Value = methodName});
         if (methodFees != null)
         {
             foreach (var methodFee in methodFees.Fees)
             {
                 if (!symbols.Contains(methodFee.Symbol) && methodFee.BasicFee > 0)
                     symbols.Add(methodFee.Symbol);
             }
             if (methodFees.IsSizeFeeFree)
             {
                 return symbols;
             }
         }

         return symbols;
     }
     
     private List<string> GetTransactionFeeSymbols(string methodName)
     {
         var actualFee = GetActualFee(methodName);
         var symbols = new List<string>();
         if (actualFee.Fees != null)
         {
             symbols = actualFee.Fees.Select(fee => fee.Symbol).Distinct().ToList();
         }

         if (!actualFee.IsSizeFeeFree)
         {
             var sizeFeeSymbols = GetSizeFeeSymbols().SymbolsToPayTxSizeFee;

             foreach (var sizeFee in sizeFeeSymbols)
             {
                 if (!symbols.Contains(sizeFee.TokenSymbol))
                     symbols.Add(sizeFee.TokenSymbol);
             }
         }

         return symbols;
     }


     private List<string> GetTransactionFeeFreeAllowancesSymbolList()
     {
         var output = State.TokenContract.GetTransactionFeeFreeAllowancesConfig.Call(new Empty());
         var symbols = output.Value.Select(l => l.Symbol).ToList();
         return symbols;
     }

     private UserContractMethodFees GetActualFee(string methodName)
     {
         var UserContractMethodFeeKey = "UserContractMethodFee";
         //configuration_key:UserContractMethod_contractAddress_methodName
         var spec = State.ConfigurationContract.GetConfiguration.Call(new StringValue
         {
             Value = $"{UserContractMethodFeeKey}_{Context.Self}_{methodName}"
         });
         var fee = new UserContractMethodFees();
         if (!spec.Value.IsNullOrEmpty())
         {
             fee.MergeFrom(spec.Value);
             return fee;
         }

         //If special key is null,get the normal fee set by the configuration contract.
         //configuration_key:UserContractMethod
         var value = State.ConfigurationContract.GetConfiguration.Call(new StringValue
         {
             Value = UserContractMethodFeeKey
         });
         if (value.Value.IsNullOrEmpty())
         {
             return new UserContractMethodFees();
         }

         fee.MergeFrom(value.Value);
         return fee;
     }
     
     private List<Address> GetDelegateeList(Address delegator, Address to, string methodName)
     {
         var allDelegatees = State.TokenContract.GetTransactionFeeDelegateeList.Call(
             new GetTransactionFeeDelegateeListInput
             {
                 DelegatorAddress = delegator,
                 ContractAddress = to,
                 MethodName = methodName
             }).DelegateeAddresses;

         if (allDelegatees == null || allDelegatees.Count == 0)
         {
             allDelegatees = State.TokenContract.GetTransactionFeeDelegatees.Call(new GetTransactionFeeDelegateesInput
             {
                 DelegatorAddress = delegator
             }).DelegateeAddresses;
         }

         return allDelegatees.ToList();
     }
}