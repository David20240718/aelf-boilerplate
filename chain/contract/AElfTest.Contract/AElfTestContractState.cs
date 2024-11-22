using System.Collections.Generic;
using AElf.Sdk.CSharp.State;
// using AElf.Standards.ACS5;
using AElf.Types;

namespace AElfTest.Contract;

public partial class AElfTestContractState : ContractState
{
    public SingletonState<Address> Owner { get; set; }
    public MappedState<string, TestTokenInfo> TokenInfoMap { get; set; }
    
    public MappedState<string, TokenInfo> TokenInfos { get; set; }
    
    /// <summary>
    ///     symbol -> address -> is in white list.
    /// </summary>
    public MappedState<string, Address, bool> LockWhiteLists { get; set; }
    
    public MappedState<Address, Address, string, long> Allowances { get; set; }
    
    
    // Alias -> Actual Symbol
    public MappedState<string, string> SymbolAliasMap { get; set; }

    public MappedState<Address, string, long> BalanceMap { get; set; }
    // public MappedState<string, MethodCallingThreshold> MethodCallingThresholds { get; set; }
    public SingletonState<AccountList> AccountList { get; set; } 
    
    public MappedState<string, long> LongKeyMap { get; set; }

}