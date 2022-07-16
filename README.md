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
`.iv-stats [97]` - Posts Pokemon IV statistics that meet the provided minimum IV argument.  