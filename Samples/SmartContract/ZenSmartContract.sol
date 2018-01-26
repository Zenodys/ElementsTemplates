pragma solidity 0.4.19;

contract ZenSmartContract {
	address owner;
        int currentTvConsumption;
	int currentWashingMachineConsumption;

	function ZenSmartContract() {
		owner = msg.sender;
	}
	
	modifier onlyOwner {
		require(msg.sender == owner);
		_;    
	}

	function getConsumptions() public 
	returns (int sumConsumption)
	{
	    return currentTvConsumption + currentWashingMachineConsumption;
	}

        function saveConsumptions(int tvConsumption, int washingMachineConsumption) public
	onlyOwner() 
	{
		currentTvConsumption = tvConsumption;
		currentWashingMachineConsumption = washingMachineConsumption;	
	}
 }
