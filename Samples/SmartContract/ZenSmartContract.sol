pragma solidity 0.4.18;

contract ZenSmartContract {
	address owner;
        int currentTvConsumption;
	int currentWashingMachineConsumption;

	function ZenSmartContract(int tvConsumption, 
               int washingMachineConsumption) {
		
		owner = msg.sender;
		currentTvConsumption = tvConsumption;
       		currentWashingMachineConsumption = washingMachineConsumption;
	}
	
	modifier onlyOwner {
		require(msg.sender == owner);
		_;    
	}

	function getConsumptions() public 
	returns (int sumConsumption) {
	    return currentTvConsumption + currentWashingMachineConsumption;
	}

        function saveConsumptions(int tvConsumption, int washingMachineConsumption) public
	onlyOwner() {
		currentTvConsumption = tvConsumption;
		currentWashingMachineConsumption = washingMachineConsumption;	
	}
 }
