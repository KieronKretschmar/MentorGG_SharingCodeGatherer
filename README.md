# SharingCodeGatherer
Polls the Valve API for new sharingcodes of users.

## HTTP Routes

### GET `/users/<steamId>`
Gets the database entry of the SharingCode user with the given steamId.

### POST `/users/<steamId>`
Create a new SharingCode user

### DELETE `/users/<steamId>`
Removes User from database.

### POST `/users/<steamId>/look-for-matches`
Triggers calls to the Steam API to find new matches of the specified user, and initiates the process of analyzing them.

## Enviroment Variables

- `STEAM_API_KEY` : 
Steam API Key, required for [Valve's Match History API](https://developer.valvesoftware.com/wiki/Counter-Strike:_Global_Offensive_Access_Match_History) [*]
- `AMQP_URI` : URI to the rabbit cluster [*]
- `AMQP_SHARINGCODE_QUEUE` : Rabbit queue's name for producing messages to SharingCodeService [*]
- `MYSQL_CONNECTION_STRING` : Connection string to SharingCodeGatherer DB[*]

[*] *Required*
