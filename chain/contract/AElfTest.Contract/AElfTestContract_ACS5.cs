// using AElf.Standards.ACS5;
// using Google.Protobuf.WellKnownTypes;
//
// namespace AElfTest.Contract;
//
// public partial class AElfTestContract
// {
//     public override Empty SetMethodCallingThreshold(SetMethodCallingThresholdInput input)
//     {
//         Assert(State.Owner.Value == Context.Sender, "No permission.");
//         if (State.MethodCallingThresholds[input.Method] == null)
//         {
//             State.MethodCallingThresholds[input.Method] = new MethodCallingThreshold
//             {
//                 SymbolToAmount = {input.SymbolToAmount}
//             };
//         }
//         else
//         {
//             foreach (var (key, value) in input.SymbolToAmount)
//             {
//                 State.MethodCallingThresholds[input.Method].SymbolToAmount[key] = value;
//             }
//         }
//
//
//         return new Empty();
//     }
//
//     public override MethodCallingThreshold GetMethodCallingThreshold(StringValue input)
//     {
//         return State.MethodCallingThresholds[input.Value];
//     }
// }