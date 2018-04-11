pragma solidity ^0.4.21;
contract AssetsLicence {
    
    address _owner;
    mapping (bytes32=>Licence) public _licences;
    string _publicKey;
    
    struct Licence{
        address customer;
		uint256 price; 
		uint256 quantity;
    }
    
    modifier onlyOwner {
		require(msg.sender == _owner);
		_;    
	}
	
	function AssetsLicence(string publicKey) public {
		_owner = msg.sender;
		_publicKey = publicKey;
	}
	
	function getPublicKey() public view returns(string publicKey){
	    return _publicKey;
	}
	
    function addLicence(bytes32 licenceId, address customer, uint256 price, uint256 quantity) public onlyOwner() returns (bool success) {
       var lic = Licence(customer, price, quantity);
       _licences[licenceId] = lic;
       return true;
    }
    
    function checkLicence(bytes32 licenceId, address customer) public view onlyOwner() returns(bool licValid) {
        return _licences[licenceId].customer == customer;
    }
    
    function confirmTransaction (bytes32 licenceId) public onlyOwner() returns(bool success) {
        _licences[licenceId].quantity = _licences[licenceId].quantity - 1;
        if (_licences[licenceId].quantity == 0)
            delete _licences[licenceId];
        
        return true;
    }
}