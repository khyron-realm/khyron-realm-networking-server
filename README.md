<!-- PROJECT LOGO -->
<br />
<p align="center">
  <a href="https://github.com/target-software/Unlimited-NetworkingServer-MiningGame">
    <img src="Images/logo.png" alt="Logo" width="81" height="80">
  </a>

  <h3 align="center">Networking Server (w/ DarkRift Networking)</h3>

  <p align="center">
    I - Mining game [Unlimited]
    <br />
    <a href="https://github.com/target-software/Unlimited-NetworkingServer-MiningGame"><strong>Explore the docs »</strong></a>
    <br />
    <br />
    <a href="https://github.com/target-software/Unlimited-NetworkingServer-MiningGame">View Demo</a>
    ·
    <a href="https://github.com/target-software/Unlimited-NetworkingServer-MiningGame/issues">Report Bug</a>
    ·
    <a href="https://github.com/target-software/Unlimited-NetworkingServer-MiningGame/issues">Request Feature</a>
  </p>
</p>


<!-- TABLE OF CONTENTS -->
<details open="open">
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#built-with">Built Using</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#project-structure">Project Structure</a></li>
    <li><a href="#necessary-libraries">Necessary Libraries</a></li>
    <li><a href="#references">References</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## About The Project

Test repository contains the Networking Server for the Unlimited Mining Game made with DarkRift 2 Server.

### Built Using

* [Darkrift networking PRO v2.9.1](https://www.darkriftnetworking.com/darkrift2)


<!-- GETTING STARTED -->
## Getting Started

### Installation

1. Clone the repo
   ```sh
   git clone https://github.com/target-software/Unlimited-NetworkingServer-MiningGame.git
   ```
2. Go into folder
    ```sh
   cd Unlimited-NetworkingtServer-MiningGame
   ```
3. Build the DLL
   ```sh
   Unlimited-MiningGame-Plugin.dll
   ```
4. Copy the DLL into the [Darkrift Server Console](https://github.com/target-software/Unlimited-DarkriftServer-MiningGame)
5. Run the [Darkrift Server](https://github.com/target-software/Unlimited-DarkriftServer-MiningGame)


<!-- USAGE EXAMPLES -->
## Usage

More detailes can be found on [Google Drive](https://docs.google.com/document/d/1CHdDfEm5BDM8vAbeubNgLF-Et8YwMgCbreD4CC6dSfo/edit)


<!-- ROADMAP -->
## Roadmap

See the [open issues](https://github.com/target-software/Unlimited-NetworkingServer-MiningGame/issues) for a list of proposed features (and known issues).


<!-- CONTRIBUTING -->
## Project structure

```bash
< PROJECT ROOT >
    |
    |-- Auction
    |    | -- AuctionRoom.cs            # Auction room structure
    |    | -- AuctionPlugin.cs          # Auction Plugin for handling auctions
    |    | -- Bid.cs                    # Bid structure
    |    | -- Player.cs                 # Player structure
    |-- Chat
    |    | -- ChatGroup.cs              # Chat group structure
    |    | -- ChatPlugin.cs             # Chat Plugin for handling the chat
    |-- Database
    |    | -- DatabaseProxy.cs          # DB connection for reading/writing
    |    | -- IDataLayer.cs             # Interface for the database layer
    |    | -- IFriendList.cs            # Interface for the friend list
    |    | -- IUser.cs                  # Interface for the user
    |-- Friends
    |    | -- FriendsList.cs            # Friend list structure
    |    | -- FriendsListDto.cs         # Friend list DTO
    |    | -- FriendsPlugin.cs          # Friends Plugin for handling friends
    |-- Game
    |    | -- Constants.cs              # Constants for the game
    |    | -- GameData.cs               # Game data structure
    |    | -- GamePlugin.cs             # Game Plugin for handling the game
    |    | -- NameGenerator.cs          # Name generator for auction/mine names
    |-- Headquarters
    |    | -- BuildTask.cs              # Build task structure
    |    | -- HeadquartersPlugin.cs     # Headquarters Plugin for handling hq
    |    | -- PlayerData.cs             # Player data structure
    |    | -- Resource.cs               # Resource structure
    |    | -- Robot.cs                  # Robot structure
    |    | -- TaskType.cs               # Task type
    |-- Login
    |    | -- Encryption.cs             # Decryption method
    |    | -- LoginPlugin.cs            # Login Plugin for handling the login
    |    | -- User.cs                   # User structure
    |-- Mines
    |    | -- Mine.cs                   # Mine structure
    |    | -- MineGenerator.cs          # Mine generator
    |    | -- MinePlugin.cs             # Mine Plugin for handling the mines
    |    | -- MineScan.cs               # Mine scan structure
    |    | -- ResourcesData.cs          # Resources data structure
    |-- MongoDBConnector
    |    | -- DataLayer.cs              # Data layer for MongoDB database
    |    | -- MongoDBPlugin.cs          # MongoDB Plugin for handling MongoDB
    |-- Tags
    |    | -- AuctionTags.cs            # Tags for auction rooms
    |    | -- ChatTags.cs               # Tags for game messages
    |    | -- FriendsTags.cs            # Tags for login messages
    |    | -- HeadquartersTags.cs       # Tags for headquarters messages
    |    | -- LoginTags.cs              # Tags for login messages
    |    | -- MineTags.cs               # Tags for mines
    |    | -- Tags.cs                   # Tags structure
    |
    |-- packages.config                 # Configuration for needed packages
    |
    |-- Bin / Debug /
    |    | -- Unlimited-NetworkingServer-MiningGame.dll
    |
    |-- README.MD                       # Readme file
    |
    |-- ************************************************************************
```


<!-- LIBRARIES -->
## Necessary Libraries

1. [Darkrift DLLs](https://assetstore.unity.com/packages/tools/network/darkrift-networking-2-95309)


<!-- REFERENCES -->
## References

1. Darkrift example [Darkrift2_Boilerplate](https://github.com/mwage/DarkRift2_Boilerplate)
1. Project Template adapted from [Othneil Drew](https://github.com/othneildrew) / [Best-README-Template](https://github.com/othneildrew/Best-README-Template).
