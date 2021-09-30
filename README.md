<!-- PROJECT LOGO -->
<br />
<p align="center">
  <a href="https://github.com/target-software/Unlimited-NetworkingServer-MiningGame">
    <img src="Images/logo.png" alt="Logo" width="80" height="80">
  </a>

  <h3 align="center">Networking Server (w/ Darkrift Networking)</h3>

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

Test repository contains the Networking Server for the Unlimited Mining Game made with Darkrift 2 Server.

### Built Using

* [Darkrift networking](https://www.darkriftnetworking.com/darkrift2)


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
    |-- Database
    |    | -- DatabaseConnector.cs              # Database connection for reading/writing data
    |-- Game
    |    |-- Player.cs                          # Player data class
    |    |-- UnlimitedPlayerPlugin.cs           # Manager for connecting players
    |-- Login
    |    |-- Encryption.cs                      # Decryption method
    |    |-- Login.cs                           # User authentication methods
    |-- Tags
    |    |-- Tags.cs                            # Tags structure
    |    |-- GameTags.cs                        # Tags for game messages
    |    |-- LoginTags.cs                       # Tags for login messages
    |
    |-- packages.config                         # Configuration for needed packages
    |
    |-- Bin / Debug /                           # Server plugin build (dll)
    |    | -- Unlimited-NetworkingServer-MiningGame.dll
    |
    |-- README.MD                               # Readme file
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
