# SharingCodeGatherer
Polls the Valve API for new sharingcodes of users.

## Enviroment Variables

- `STEAM_API_KEY` : 
Steam API Key, required for [Valve's Match History API](https://developer.valvesoftware.com/wiki/Counter-Strike:_Global_Offensive_Access_Match_History) [*]
- `AMQP_URI` : URI to the rabbit cluster [*]
- `AMQP_SHARINGCODE_QUEUE` : Rabbit queue's name for producing messages to SharingCodeService [*]
- `MYSQL_CONNECTION_STRING` : Connection string to SharingCodeGatherer DB[*]

- `IS_MIGRATING` : Boolean to indicate if migration is active

[*] *Required*
