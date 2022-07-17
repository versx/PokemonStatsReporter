[![Build](https://github.com/versx/PokemonStatsReporter/workflows/.NET/badge.svg)](https://github.com/versx/PokemonStatsReporter/actions)
[![GitHub Release](https://img.shields.io/github/release/versx/PokemonStatsReporter.svg)](https://github.com/versx/PokemonStatsReporter/releases/)
[![Discord](https://img.shields.io/discord/552003258000998401.svg?label=&logo=discord&logoColor=ffffff&color=7389D8&labelColor=6A7EC2)](https://discord.gg/zZ9h9Xa)  

# Pokemon Statistics Reporter  
Reports nightly at midnight all shiny, hundo, or custom IV statistics about Pokemon found from the previous day.  

## Prerequisites  
- [.NET 6 SDK or higher](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)  

## Getting Started  
1. `git clone https://github.com/versx/PokemonStatsReporter && cd PokemonStatsReporter`  
1. `dotnet build`  
1. `cp config.example.json bin/config.json`  
1. `cp -R static bin/static/`  
1. Fill out the `bin/config.json` file.  
1. Start PokemonStatsReporter from the `bin` folder: `dotnet PokemonStatsReporter.dll`.  

## Available Commands  
`.shiny-stats` - Posts shiny Pokemon statistics  
`.hundo-stats` - Posts hundred percent Pokemon statistics  
`.iv-stats [97]` - Posts Pokemon IV statistics that meet the provided minimum IV argument. (default: 100)  

## Configuration  
```json
{
    // Language to use for translations
    "locale": "en",
    // Amount of hours to check of previous data that will be included in statistic reports
    "statHours": 24,
    // Discord servers that will receive the statistic reporting
    "servers": {
        // Discord server guild ID
        "0000000000": {
            // Discord bot configuration
            "bot": {
                // Discord commands prefix, leave empty to use bot mention prefix, i.e. `@StatsBot#123 shiny-stats`
                "commandPrefix": ".",
                // Discord bot token from discord.com/developers
                "token": "<DISCORD_BOT_TOKEN>",
                // Discord bot status to display in user list
                "status": "Reporting Pokemon..."
            },
            // Daily statistic reporting configuration
            "dailyStats": {
                // Shiny Pokemon statistics
                "shiny": {
                    // Whether to report shiny stats or not
                    "enabled": false,
                    // Delete any previous channel messages before reporting statistics
                    "clearMessages": false,
                    // Channel ID that will receive the shiny statistics report
                    "channelId": 0000000000
                },
                // 100% IV Pokemon statistics
                "hundo": {
                    // Whether to report hundo stats or not
                    "enabled": false,
                    // Delete any previous channel messages before reporting statistics
                    "clearMessages": false,
                    // Channel ID that will receive the hundo statistics report
                    "channelId": 0000000000
                },
                // Custom IV Pokemon statistics
                "iv": {
                    // Whether to report custom IV stats or not
                    "enabled": false,
                    // Delete any previous channel messages before reporting statistics
                    "clearMessages": false,
                    // Channel ID that will receive the custom IV statistics report
                    "channelId": 0000000000,
                    // Minimum IV Pokemon must meet to be included in custom IV statistics report
                    "minimumIV": 100
                }
            }
        }
    },
    /* Database configuration */
    "database": {
        // Database server hostname/IP address
        "host": "127.0.0.1",
        // Database server listening port
        "port": 3306,
        // Database server username
        "username": "rdmuser",
        // Database server password for above user account
        "password": "pass123!",
        // Database server table to fetch statistics from
        "database": "rdmdb"
    }
}
```