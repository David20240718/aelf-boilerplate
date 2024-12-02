
using System.Text;
using System.Text.RegularExpressions;
using AElf;
using AElf.Types;
using Google.Protobuf;

namespace AElfTest.Contract;

public partial class AElfTestContract
{
    private bool IsValidCreateSymbol(string symbol)
    {
        return Regex.IsMatch(symbol, "^[a-zA-Z0-9]+$");
    }
    
    private bool IsValidItemId(string symbolItemId)
    {
        return Regex.IsMatch(symbolItemId, "^[0-9]+$");
    }
    
    
    private void AssertValidMemo(string memo)
    {
        Assert(memo == null || Encoding.UTF8.GetByteCount(memo) <= AElfTestTokenContractConstants.MemoMaxLength,
            "Invalid memo size.");
    }
    
    private void AssertValidInputAddress(Address input)
    {
        Assert(input != null && !input.Value.IsNullOrEmpty(), "Invalid input address.");
    }
    
    private TokenInfo AssertValidToken(string symbol, long amount)
    {
        AssertValidSymbolAndAmount(symbol, amount);
        var tokenInfo = GetTokenInfo(symbol);
        Assert(tokenInfo != null && !string.IsNullOrEmpty(tokenInfo.Symbol), $"Token is not found. {symbol}");
        return tokenInfo;
    }
    
            
    private void FireExternalLogEvent(TokenInfo tokenInfo, TransferFromInput input)
    {
        if (tokenInfo.ExternalInfo.Value.ContainsKey(AElfTestTokenContractConstants.LogEventExternalInfoKey))
            Context.FireLogEvent(new LogEvent
            {
                Name = tokenInfo.ExternalInfo.Value[AElfTestTokenContractConstants.LogEventExternalInfoKey],
                Address = Context.Self,
                NonIndexed = input.ToByteString()
            });
    }
    
    private void AssertValidApproveTokenAndAmount(string symbol, long amount)
    {
        Assert(amount > 0, "Invalid amount.");
        AssertApproveToken(symbol);
    }
    
    private void AssertApproveToken(string symbol)
    {
        Assert(!string.IsNullOrEmpty(symbol), "Symbol can not be null.");
        var words = symbol.Split(AElfTestTokenContractConstants.NFTSymbolSeparator);
        var symbolPrefix = words[0];
        var allSymbolIdentifier = GetAllSymbolIdentifier();
        Assert(symbolPrefix.Length > 0 && (IsValidCreateSymbol(symbolPrefix) || symbolPrefix.Equals(allSymbolIdentifier)), "Invalid symbol.");
        if (words.Length == 1)
        {
            if (!symbolPrefix.Equals(allSymbolIdentifier))
            {
                ValidTokenExists(symbolPrefix);
            }
            return;
        }
        Assert(words.Length == 2, "Invalid symbol length.");
        var itemId = words[1];
        Assert(itemId.Length > 0 && (IsValidItemId(itemId) || itemId.Equals(allSymbolIdentifier)), "Invalid NFT Symbol.");
        var nftSymbol = itemId.Equals(allSymbolIdentifier) ? GetCollectionSymbol(symbolPrefix) : symbol;
        ValidTokenExists(nftSymbol);
    }
    
    private void ValidTokenExists(string symbol)
    {
        var tokenInfo = State.TokenInfos[symbol];
        Assert(tokenInfo != null && !string.IsNullOrEmpty(tokenInfo.Symbol),
            $"Token is not found. {symbol}");
    }
    
    private string GetCollectionSymbol(string symbolPrefix)
    {
        return $"{symbolPrefix}-{AElfTestTokenContractConstants.CollectionSymbolSuffix}";
    }
}