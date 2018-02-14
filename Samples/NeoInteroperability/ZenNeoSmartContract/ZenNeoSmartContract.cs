using Neo.SmartContract.Framework.Services.Neo;

namespace Neo.SmartContract
{
    public class ZenNeoSmartContract : Framework.SmartContract
    {
        public static int Main(int consumptions)
        {
            Storage.Put(Storage.CurrentContext, "consumptions", consumptions);
            return consumptions;
        }
    }
}
