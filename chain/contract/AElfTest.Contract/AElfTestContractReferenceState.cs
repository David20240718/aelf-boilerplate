using AElf.Contracts.Configuration;
using AElf.Contracts.MultiToken;
namespace AElfTest.Contract;

public partial class AElfTestContractState
{
    internal TokenContractImplContainer.TokenContractImplReferenceState TokenContract { get; set; }
    
    internal ConfigurationContainer.ConfigurationReferenceState ConfigurationContract { get; set; }
    
}