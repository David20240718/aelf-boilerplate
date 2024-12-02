using System;
using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElfTest.Contract;

public partial class AElfTestContract
{
    public override BoolValue IsInWhiteList(IsInWhiteListInput input)
    {
        return new BoolValue { Value = State.LockWhiteLists[input.Symbol][input.Address] };
    }
    
    private string GetActualTokenSymbol(string aliasOrSymbol)
    {
        if (State.TokenInfos[aliasOrSymbol] == null)
        {
            return State.SymbolAliasMap[aliasOrSymbol] ?? aliasOrSymbol;
        }

        return aliasOrSymbol;
    }
}