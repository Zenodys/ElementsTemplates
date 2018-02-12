using Neo.SmartContract.Framework.Services.Neo;

namespace Neo.SmartContract
{
    public class ZenNeoSmartContract : Framework.SmartContract
    {
        public static int Main(int tvConsumption, int washingMachineConsumption)
        {
            Storage.Put(Storage.CurrentContext, "consumption", tvConsumption + washingMachineConsumption);
            return tvConsumption + washingMachineConsumption;
        }
    }
}
