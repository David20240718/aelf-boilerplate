using System.Linq;

namespace AElfTest.Contract;

public partial class AElfTestContract
{
    private SymbolType GetSymbolType(string symbol)
    {
        var words = symbol.Split(AElfTestTokenContractConstants.NFTSymbolSeparator);
        Assert(words[0].Length > 0 && IsValidCreateSymbol(words[0]), "Invalid Symbol input");
        if (words.Length == 1) return SymbolType.Token;
        Assert(words.Length == 2 && words[1].Length > 0 && IsValidItemId(words[1]), "Invalid NFT Symbol input");
        return words[1] == AElfTestTokenContractConstants.CollectionSymbolSuffix ? SymbolType.NftCollection : SymbolType.Nft;
    }

}